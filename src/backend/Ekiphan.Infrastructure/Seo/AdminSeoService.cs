using Ekiphan.Application.Catalog;
using Ekiphan.Application.Seo;
using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo;

internal sealed class AdminSeoService(
    EkiphanDbContext db,
    IPublicMediaUrlResolver mediaUrlResolver) : IAdminSeoService
{
    public async Task<AdminSeoPage> GetPageAsync(
        AdminSeoQuery query,
        AdminSeoAccessScope scope,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var rows = Build(scope);
        if (query.ContentType.HasValue)
        {
            EnsureAccess(query.ContentType.Value, scope);
            rows = rows.Where(x => x.ContentType == query.ContentType.Value);
        }
        if (query.Language is { Length: > 0 })
        {
            var language = NormalizeLanguage(query.Language);
            rows = rows.Where(x => x.Language == language);
        }
        if (query.Published.HasValue) rows = rows.Where(x => x.Published == query.Published);
        if (query.MissingMetaTitle.HasValue) rows = rows.Where(x => x.MissingTitle == query.MissingMetaTitle);
        if (query.MissingMetaDescription.HasValue) rows = rows.Where(x => x.MissingDescription == query.MissingMetaDescription);
        if (query.MissingOpenGraphImage.HasValue) rows = rows.Where(x => x.MissingImage == query.MissingOpenGraphImage);
        if (query.DuplicateSlug.HasValue) rows = rows.Where(x => x.DuplicateSlug == query.DuplicateSlug);
        if (query.DuplicateMetaTitle.HasValue) rows = rows.Where(x => x.DuplicateTitle == query.DuplicateMetaTitle);
        if (query.NoIndex.HasValue) rows = rows.Where(x => x.NoIndex == query.NoIndex);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(x => x.Name.Contains(search) || x.Slug.Contains(search) || (x.MetaTitle != null && x.MetaTitle.Contains(search)));
        }

        var count = await rows.CountAsync(cancellationToken);
        rows = query.Sort switch
        {
            "name" => rows.OrderBy(x => x.Name).ThenBy(x => x.Language),
            "score" => rows.OrderBy(x => x.Score).ThenBy(x => x.Name),
            "updated" => rows.OrderBy(x => x.UpdatedAt),
            _ => rows.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Name),
        };
        var pageRows = await rows.Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize).ToListAsync(cancellationToken);
        return new AdminSeoPage(pageRows.Select(ToDto).ToArray(), query.Page, query.PageSize, count);
    }

    public async Task<AdminSeoHealth> GetHealthAsync(
        AdminSeoAccessScope scope,
        CancellationToken cancellationToken = default)
    {
        var rows = Build(scope);
        var totalIndexable = await rows.CountAsync(x => x.Published && !x.NoIndex, cancellationToken);
        var missingTitle = await rows.CountAsync(x => x.MissingTitle, cancellationToken);
        var missingDescription = await rows.CountAsync(x => x.MissingDescription, cancellationToken);
        var missingImage = await rows.CountAsync(x => x.MissingImage, cancellationToken);
        var duplicateSlugs = await rows.CountAsync(x => x.DuplicateSlug, cancellationToken);
        var duplicateTitles = await rows.CountAsync(x => x.DuplicateTitle, cancellationToken);
        var noIndex = await rows.CountAsync(x => x.NoIndex, cancellationToken);
        var missingEnglish = await rows.CountAsync(x => x.Language == "tr" && !x.HasEnglish, cancellationToken);
        var recent = await rows.OrderByDescending(x => x.UpdatedAt).Take(8).ToListAsync(cancellationToken);
        return new AdminSeoHealth(totalIndexable, missingTitle, missingDescription,
            missingImage, duplicateSlugs, duplicateTitles, noIndex, missingEnglish,
            recent.Select(ToDto).ToArray());
    }

    public async Task<IReadOnlyList<AdminSeoDuplicate>> GetDuplicatesAsync(
        AdminSeoAccessScope scope,
        CancellationToken cancellationToken = default)
    {
        var rows = Build(scope);
        var slugs = await rows.GroupBy(x => new { x.ContentType, x.Language, x.Slug })
            .Where(x => x.Count() > 1).Select(x => new AdminSeoDuplicate(
                x.Key.ContentType, x.Key.Language, "slug", x.Key.Slug, x.Count()))
            .Take(100).ToListAsync(cancellationToken);
        var titles = await rows.Where(x => x.MetaTitle != null)
            .GroupBy(x => new { x.ContentType, x.Language, x.MetaTitle })
            .Where(x => x.Count() > 1).Select(x => new AdminSeoDuplicate(
                x.Key.ContentType, x.Key.Language, "metaTitle", x.Key.MetaTitle!, x.Count()))
            .Take(100).ToListAsync(cancellationToken);
        return slugs.Concat(titles).ToArray();
    }

    public async Task<AdminSeoDetail?> GetDetailAsync(
        SeoContentType type, Guid id, AdminSeoAccessScope scope,
        CancellationToken cancellationToken = default)
    {
        EnsureAccess(type, scope);
        DetailRow? row = type switch
        {
            SeoContentType.Product => await db.Products.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new DetailRow { Published = x.IsPublished, UpdatedAt = x.UpdatedAt,
                    Translations = x.Translations.Select(t => FromProduct(t)).ToArray() }).SingleOrDefaultAsync(cancellationToken),
            SeoContentType.Category => await db.Categories.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new DetailRow { Published = x.IsPublished, UpdatedAt = x.UpdatedAt,
                    Translations = x.Translations.Select(t => FromCategory(t)).ToArray() }).SingleOrDefaultAsync(cancellationToken),
            SeoContentType.ContentPage => await db.ContentPages.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new DetailRow { Published = x.Status == ContentStatus.Published, UpdatedAt = x.UpdatedAt,
                    Translations = x.Translations.Select(t => FromContent(t)).ToArray() }).SingleOrDefaultAsync(cancellationToken),
            SeoContentType.PressRelease => await db.PressReleases.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new DetailRow { Published = x.IsPublished, UpdatedAt = x.UpdatedAt,
                    Translations = x.Translations.Select(t => FromPress(t)).ToArray() }).SingleOrDefaultAsync(cancellationToken),
            _ => null,
        };
        if (row is null) return null;
        var mediaIds = row.Translations.Where(x => x.ImageId.HasValue).Select(x => x.ImageId!.Value).Distinct().ToArray();
        var keys = await db.MediaAssets.AsNoTracking().Where(x => mediaIds.Contains(x.Id) && x.Status == MediaStatus.Active && x.AssetType == MediaAssetType.Image)
            .ToDictionaryAsync(x => x.Id, x => x.StorageKey, cancellationToken);
        return new AdminSeoDetail(type, id, row.Published, row.UpdatedAt,
            row.Translations.Select(x => new AdminSeoTranslation(x.Language, x.Name, x.Slug,
                x.MetaTitle, x.MetaDescription, x.CanonicalUrl, x.OgTitle, x.OgDescription,
                x.ImageId, x.ImageId.HasValue && keys.TryGetValue(x.ImageId.Value, out var key) ? mediaUrlResolver.Resolve(key) : null,
                x.NoIndex, x.NoFollow)).ToArray());
    }

    public async Task<AdminSeoDetail?> UpdateAsync(
        SeoContentType type, Guid id, UpdateAdminSeoCommand command,
        AdminSeoAccessScope scope, CancellationToken cancellationToken = default)
    {
        EnsureAccess(type, scope);
        if (command.Translations is null || command.Translations.Count is < 1 or > 2)
            throw new ArgumentException("Translations must use unique tr or en languages.");
        var languages = command.Translations.Select(x => NormalizeLanguage(x.Language)).ToArray();
        if (languages.Distinct(StringComparer.Ordinal).Count() != languages.Length)
            throw new ArgumentException("Translations must use unique tr or en languages.");
        await ValidateImagesAsync(command.Translations, cancellationToken);
        try
        {
            var found = await ApplyUpdateAsync(type, id, command, cancellationToken);
            if (!found) return null;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AdminSeoConflictException("Slug is already in use for this content type and language.");
        }
        return await GetDetailAsync(type, id, scope, cancellationToken);
    }

    public async Task<SlugAvailability> IsSlugAvailableAsync(
        SeoContentType type, Guid? id, string language, string slug,
        AdminSeoAccessScope scope, CancellationToken cancellationToken = default)
    {
        EnsureAccess(type, scope);
        var normalizedLanguage = language.Trim().ToLowerInvariant();
        var normalizedSlug = Ekiphan.Domain.Common.SeoValue.Slug(slug, 250);
        var exists = type switch
        {
            SeoContentType.Product => await db.Set<ProductTranslation>().AnyAsync(x => x.LanguageCode == normalizedLanguage && x.Slug == normalizedSlug && x.ProductId != id, cancellationToken),
            SeoContentType.Category => await db.Set<CategoryTranslation>().AnyAsync(x => x.LanguageCode == normalizedLanguage && x.Slug == normalizedSlug && x.CategoryId != id, cancellationToken),
            SeoContentType.ContentPage => await db.Set<ContentPageTranslation>().AnyAsync(x => x.LanguageCode == normalizedLanguage && x.Slug == normalizedSlug && x.ContentPageId != id, cancellationToken),
            SeoContentType.PressRelease => await db.Set<PressReleaseTranslation>().AnyAsync(x => x.LanguageCode == normalizedLanguage && x.Slug == normalizedSlug && x.PressReleaseId != id, cancellationToken),
            _ => true,
        };
        return new SlugAvailability(!exists);
    }

    private IQueryable<Row> Build(AdminSeoAccessScope scope)
    {
        IQueryable<Row>? rows = null;
        if (scope.Catalog)
        {
            rows = ProductRows().Concat(CategoryRows());
        }
        if (scope.Content)
        {
            var content = ContentRows().Concat(PressRows());
            rows = rows is null ? content : rows.Concat(content);
        }
        return rows ?? db.Products.Where(_ => false).Select(_ => new Row());
    }

    private IQueryable<Row> ProductRows() => db.Products.AsNoTracking().Where(x => !x.IsDeleted)
        .SelectMany(x => x.Translations.Select(t => new Row {
            ContentType = SeoContentType.Product, ContentId = x.Id, Name = t.Name, Language = t.LanguageCode,
            Slug = t.Slug, MetaTitle = t.MetaTitle, MissingTitle = t.MetaTitle == null || t.MetaTitle == "",
            MissingDescription = t.MetaDescription == null || t.MetaDescription == "", MissingImage = t.OpenGraphImageMediaId == null,
            NoIndex = t.NoIndex, Published = x.IsPublished, UpdatedAt = x.UpdatedAt,
            DuplicateSlug = db.Set<ProductTranslation>().Count(other => other.LanguageCode == t.LanguageCode && other.Slug == t.Slug) > 1,
            DuplicateTitle = t.MetaTitle != null && db.Set<ProductTranslation>().Count(other => other.LanguageCode == t.LanguageCode && other.MetaTitle == t.MetaTitle) > 1,
            HasEnglish = x.Translations.Any(other => other.LanguageCode == "en"),
            Score = 100 - (t.MetaTitle == null || t.MetaTitle == "" ? 25 : 0) - (t.MetaDescription == null || t.MetaDescription == "" ? 25 : 0) - (t.OpenGraphImageMediaId == null ? 25 : 0) - (t.CanonicalUrl == null || t.CanonicalUrl == "" ? 25 : 0)
        }));

    private IQueryable<Row> CategoryRows() => db.Categories.AsNoTracking()
        .SelectMany(x => x.Translations.Select(t => new Row {
            ContentType = SeoContentType.Category, ContentId = x.Id, Name = t.Name, Language = t.LanguageCode,
            Slug = t.Slug, MetaTitle = t.MetaTitle, MissingTitle = t.MetaTitle == null || t.MetaTitle == "",
            MissingDescription = t.MetaDescription == null || t.MetaDescription == "", MissingImage = t.OpenGraphImageMediaId == null,
            NoIndex = t.NoIndex, Published = x.IsPublished, UpdatedAt = x.UpdatedAt,
            DuplicateSlug = db.Set<CategoryTranslation>().Count(other => other.LanguageCode == t.LanguageCode && other.Slug == t.Slug) > 1,
            DuplicateTitle = t.MetaTitle != null && db.Set<CategoryTranslation>().Count(other => other.LanguageCode == t.LanguageCode && other.MetaTitle == t.MetaTitle) > 1,
            HasEnglish = x.Translations.Any(other => other.LanguageCode == "en"),
            Score = 100 - (t.MetaTitle == null || t.MetaTitle == "" ? 25 : 0) - (t.MetaDescription == null || t.MetaDescription == "" ? 25 : 0) - (t.OpenGraphImageMediaId == null ? 25 : 0) - (t.CanonicalUrl == null || t.CanonicalUrl == "" ? 25 : 0)
        }));

    private IQueryable<Row> ContentRows() => db.ContentPages.AsNoTracking()
        .SelectMany(x => x.Translations.Select(t => new Row {
            ContentType = SeoContentType.ContentPage, ContentId = x.Id, Name = t.Title, Language = t.LanguageCode,
            Slug = t.Slug, MetaTitle = t.MetaTitle, MissingTitle = t.MetaTitle == null || t.MetaTitle == "",
            MissingDescription = t.MetaDescription == null || t.MetaDescription == "", MissingImage = t.OpenGraphImageMediaId == null,
            NoIndex = t.NoIndex, Published = x.Status == ContentStatus.Published, UpdatedAt = x.UpdatedAt,
            DuplicateSlug = db.Set<ContentPageTranslation>().Count(other => other.LanguageCode == t.LanguageCode && other.Slug == t.Slug) > 1,
            DuplicateTitle = t.MetaTitle != null && db.Set<ContentPageTranslation>().Count(other => other.LanguageCode == t.LanguageCode && other.MetaTitle == t.MetaTitle) > 1,
            HasEnglish = x.Translations.Any(other => other.LanguageCode == "en"),
            Score = 100 - (t.MetaTitle == null || t.MetaTitle == "" ? 25 : 0) - (t.MetaDescription == null || t.MetaDescription == "" ? 25 : 0) - (t.OpenGraphImageMediaId == null ? 25 : 0) - (t.CanonicalUrl == null || t.CanonicalUrl == "" ? 25 : 0)
        }));

    private IQueryable<Row> PressRows() => db.PressReleases.AsNoTracking()
        .SelectMany(x => x.Translations.Select(t => new Row {
            ContentType = SeoContentType.PressRelease, ContentId = x.Id, Name = t.Title, Language = t.LanguageCode,
            Slug = t.Slug, MetaTitle = t.MetaTitle, MissingTitle = t.MetaTitle == null || t.MetaTitle == "",
            MissingDescription = t.MetaDescription == null || t.MetaDescription == "", MissingImage = t.OpenGraphImageMediaId == null,
            NoIndex = t.NoIndex, Published = x.IsPublished, UpdatedAt = x.UpdatedAt,
            DuplicateSlug = db.Set<PressReleaseTranslation>().Count(other => other.LanguageCode == t.LanguageCode && other.Slug == t.Slug) > 1,
            DuplicateTitle = t.MetaTitle != null && db.Set<PressReleaseTranslation>().Count(other => other.LanguageCode == t.LanguageCode && other.MetaTitle == t.MetaTitle) > 1,
            HasEnglish = x.Translations.Any(other => other.LanguageCode == "en"),
            Score = 100 - (t.MetaTitle == null || t.MetaTitle == "" ? 25 : 0) - (t.MetaDescription == null || t.MetaDescription == "" ? 25 : 0) - (t.OpenGraphImageMediaId == null ? 25 : 0) - (t.CanonicalUrl == null || t.CanonicalUrl == "" ? 25 : 0)
        }));

    private async Task<bool> ApplyUpdateAsync(SeoContentType type, Guid id, UpdateAdminSeoCommand command, CancellationToken ct)
    {
        if (type == SeoContentType.Product)
        {
            var x = await db.Products.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct); if (x is null) return false;
            foreach (var s in command.Translations) { var language = NormalizeLanguage(s.Language); var t = x.Translations.SingleOrDefault(y => y.LanguageCode == language) ?? throw new ArgumentException("Translation does not exist."); x.SetTranslation(language, t.Name, s.Slug, t.ShortDescription, t.LongDescription, s.MetaTitle, s.MetaDescription, s.CanonicalUrl, s.NoIndex, s.NoFollow, s.OpenGraphTitle, s.OpenGraphDescription, s.OpenGraphImageMediaId); } return true;
        }
        if (type == SeoContentType.Category)
        {
            var x = await db.Categories.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id, ct); if (x is null) return false;
            foreach (var s in command.Translations) { var language = NormalizeLanguage(s.Language); var t = x.Translations.SingleOrDefault(y => y.LanguageCode == language) ?? throw new ArgumentException("Translation does not exist."); x.SetTranslation(language, t.Name, s.Slug, t.Description, s.MetaTitle, s.MetaDescription, s.CanonicalUrl, s.NoIndex, s.NoFollow, s.OpenGraphTitle, s.OpenGraphDescription, s.OpenGraphImageMediaId); } return true;
        }
        if (type == SeoContentType.ContentPage)
        {
            var x = await db.ContentPages.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id, ct); if (x is null) return false;
            foreach (var s in command.Translations) { var language = NormalizeLanguage(s.Language); var t = x.Translations.SingleOrDefault(y => y.LanguageCode == language) ?? throw new ArgumentException("Translation does not exist."); x.SetTranslation(language, t.Title, s.Slug, t.Summary, t.Body, s.MetaTitle, s.MetaDescription, s.CanonicalUrl, s.NoIndex, s.NoFollow, s.OpenGraphTitle, s.OpenGraphDescription, s.OpenGraphImageMediaId); } return true;
        }
        if (type == SeoContentType.PressRelease)
        {
            var x = await db.PressReleases.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id, ct); if (x is null) return false;
            foreach (var s in command.Translations) { var language = NormalizeLanguage(s.Language); var t = x.Translations.SingleOrDefault(y => y.LanguageCode == language) ?? throw new ArgumentException("Translation does not exist."); x.SetTranslation(language, t.Title, t.Summary, t.Body, s.Slug, s.MetaTitle, s.MetaDescription, s.CanonicalUrl, s.NoIndex, s.NoFollow, s.OpenGraphTitle, s.OpenGraphDescription, s.OpenGraphImageMediaId); } return true;
        }
        return false;
    }

    private async Task ValidateImagesAsync(IReadOnlyList<AdminSeoTranslation> items, CancellationToken ct)
    {
        var ids = items.Where(x => x.OpenGraphImageMediaId.HasValue).Select(x => x.OpenGraphImageMediaId!.Value).Distinct().ToArray();
        if (ids.Length == 0) return;
        var count = await db.MediaAssets.CountAsync(x => ids.Contains(x.Id) && x.Status == MediaStatus.Active && x.AssetType == MediaAssetType.Image, ct);
        if (count != ids.Length) throw new ArgumentException("Open Graph images must reference active image media.");
    }

    private static AdminSeoRow ToDto(Row x) => new(x.ContentType, x.ContentId, x.Name, x.Language, x.Slug,
        x.MetaTitle, x.Score, x.NoIndex, x.Published, x.UpdatedAt,
        $"/admin/seo/{x.ContentType.ToString().ToLowerInvariant()}/{x.ContentId}");
    private static void ValidateQuery(AdminSeoQuery query) { if (query.Page < 1 || query.PageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(query)); }
    private static string NormalizeLanguage(string value)
    {
        var language = value.Trim().ToLowerInvariant();
        return language is "tr" or "en"
            ? language
            : throw new ArgumentException("Language must be tr or en.", nameof(value));
    }
    private static void EnsureAccess(SeoContentType type, AdminSeoAccessScope scope) { if ((type is SeoContentType.Product or SeoContentType.Category && !scope.Catalog) || (type is SeoContentType.ContentPage or SeoContentType.PressRelease && !scope.Content)) throw new UnauthorizedAccessException(); }

    private static TranslationRow FromProduct(ProductTranslation t) => new() { Language=t.LanguageCode, Name=t.Name, Slug=t.Slug, MetaTitle=t.MetaTitle, MetaDescription=t.MetaDescription, CanonicalUrl=t.CanonicalUrl, OgTitle=t.OpenGraphTitle, OgDescription=t.OpenGraphDescription, ImageId=t.OpenGraphImageMediaId, NoIndex=t.NoIndex, NoFollow=t.NoFollow };
    private static TranslationRow FromCategory(CategoryTranslation t) => new() { Language=t.LanguageCode, Name=t.Name, Slug=t.Slug, MetaTitle=t.MetaTitle, MetaDescription=t.MetaDescription, CanonicalUrl=t.CanonicalUrl, OgTitle=t.OpenGraphTitle, OgDescription=t.OpenGraphDescription, ImageId=t.OpenGraphImageMediaId, NoIndex=t.NoIndex, NoFollow=t.NoFollow };
    private static TranslationRow FromContent(ContentPageTranslation t) => new() { Language=t.LanguageCode, Name=t.Title, Slug=t.Slug, MetaTitle=t.MetaTitle, MetaDescription=t.MetaDescription, CanonicalUrl=t.CanonicalUrl, OgTitle=t.OpenGraphTitle, OgDescription=t.OpenGraphDescription, ImageId=t.OpenGraphImageMediaId, NoIndex=t.NoIndex, NoFollow=t.NoFollow };
    private static TranslationRow FromPress(PressReleaseTranslation t) => new() { Language=t.LanguageCode, Name=t.Title, Slug=t.Slug, MetaTitle=t.MetaTitle, MetaDescription=t.MetaDescription, CanonicalUrl=t.CanonicalUrl, OgTitle=t.OpenGraphTitle, OgDescription=t.OpenGraphDescription, ImageId=t.OpenGraphImageMediaId, NoIndex=t.NoIndex, NoFollow=t.NoFollow };

    private sealed class Row { public SeoContentType ContentType { get; set; } public Guid ContentId { get; set; } public string Name { get; set; }=""; public string Language { get; set; }=""; public string Slug { get; set; }=""; public string? MetaTitle { get; set; } public bool MissingTitle { get; set; } public bool MissingDescription { get; set; } public bool MissingImage { get; set; } public bool DuplicateSlug { get; set; } public bool DuplicateTitle { get; set; } public bool HasEnglish { get; set; } public bool NoIndex { get; set; } public bool Published { get; set; } public int Score { get; set; } public DateTimeOffset UpdatedAt { get; set; } }
    private sealed class TranslationRow { public string Language { get; set; }=""; public string Name { get; set; }=""; public string Slug { get; set; }=""; public string? MetaTitle { get; set; } public string? MetaDescription { get; set; } public string? CanonicalUrl { get; set; } public string? OgTitle { get; set; } public string? OgDescription { get; set; } public Guid? ImageId { get; set; } public bool NoIndex { get; set; } public bool NoFollow { get; set; } }
    private sealed class DetailRow { public bool Published { get; set; } public DateTimeOffset UpdatedAt { get; set; } public TranslationRow[] Translations { get; set; }=[]; }
}
