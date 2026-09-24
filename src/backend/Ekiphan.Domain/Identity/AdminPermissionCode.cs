namespace Ekiphan.Domain.Identity;

public static class AdminPermissionCode
{
    public static class Products { public const string Read="products.read", Create="products.create", Update="products.update", Publish="products.publish", PublishDirect="products.publish.direct", Archive="products.archive", Delete="products.delete", Import="products.import", ImportRollback="products.import.rollback", BulkUpdate="products.bulk.update", HistoryRead="products.history.read", HistoryRestore="products.history.restore", QualityRead="products.quality.read", QualityRecalculate="products.quality.recalculate"; }
    public static class Categories { public const string Read="categories.read", Create="categories.create", Update="categories.update", Reorder="categories.reorder", Archive="categories.archive", Delete="categories.delete"; }
    public static class Brands { public const string Read="brands.read", Create="brands.create", Update="brands.update", Archive="brands.archive", Delete="brands.delete"; }
    public static class Media { public const string Read="media.read", Upload="media.upload", Update="media.update", Process="media.process", Import="media.import", ImportRollback="media.import.rollback", Archive="media.archive", Delete="media.delete"; }
    public static class Quotes { public const string Read="quotes.read", Assign="quotes.assign", UpdateStatus="quotes.update.status", NotesCreate="quotes.notes.create", NotesUpdate="quotes.notes.update", NotesDelete="quotes.notes.delete", Export="quotes.export", PdfGenerate="quotes.pdf.generate", EmailRetry="quotes.email.retry", Archive="quotes.archive", Delete="quotes.delete", HistoryRead="quotes.history.read"; }
    public static class Content { public const string Read="content.read", Create="content.create", Update="content.update", Publish="content.publish", Archive="content.archive", Delete="content.delete", Preview="content.preview", Schedule="content.schedule", HistoryRead="content.history.read", HistoryRestore="content.history.restore"; }
    public static class References { public const string Read="references.read", Create="references.create", Update="references.update", Publish="references.publish", Archive="references.archive", Manage="references.manage"; }
    public static class Showroom { public const string Read="showroom.read", Manage="showroom.manage", Publish="showroom.publish"; }
    public static class Banners { public const string Read="banners.read", Manage="banners.manage", Publish="banners.publish"; }
    public static class Seo { public const string Read="seo.read", Manage="seo.manage", RedirectsManage="seo.redirects.manage", RobotsManage="seo.robots.manage", AuditRead="seo.audit.read"; }
    public static class Users { public const string Read="users.read", Create="users.create", Update="users.update", Disable="users.disable", Delete="users.delete", SessionsManage="users.sessions.manage"; }
    public static class Roles { public const string Read="roles.read", Create="roles.create", Update="roles.update", Delete="roles.delete", Assign="roles.assign"; }
    public static class Permissions { public const string Read="permissions.read", Assign="permissions.assign"; }
    public static class Settings { public const string Read="settings.read", Manage="settings.manage"; }
    public static class Audit { public const string Read="audit.read", DetailsRead="audit.details.read"; }
    public static class Security { public const string TwoFactorManage="security.2fa.manage", LoginHistoryRead="security.login-history.read", SessionsRead="security.sessions.read", SessionsRevoke="security.sessions.revoke", DashboardRead="security.dashboard.read", EventsRead="security.events.read", EventsManage="security.events.manage"; }
    public static class System { public const string HealthRead="system.health.read"; }

    // Legacy codes remain accepted while endpoints migrate to granular policies.
    public const string CatalogManage="catalog.manage", QuotesManage="quotes.manage", ContactsRead="contacts.read", ContactsManage="contacts.manage", ImportsManage="imports.manage", ImportsPublish="imports.publish", MediaManage="media.manage", UsersManage="users.manage", ContentManage="content.manage";
    public const string ProductsImport=Products.Import, ProductsImportRollback=Products.ImportRollback,
        MediaImport=Media.Import, MediaImportRollback=Media.ImportRollback,
        MediaRead=Media.Read, MediaUpload=Media.Upload, MediaProcess=Media.Process,
        MediaRetry="media.retry", MediaArchive=Media.Archive, QuotesRead=Quotes.Read;

    public static readonly IReadOnlySet<string> Granular = new HashSet<string>(typeof(AdminPermissionCode)
        .GetNestedTypes().SelectMany(t => t.GetFields(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static))
        .Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()!), StringComparer.Ordinal);
    public static readonly IReadOnlySet<string> All = new HashSet<string>(Granular.Concat(
        [CatalogManage, QuotesManage, ContactsRead, ContactsManage, ImportsManage, ImportsPublish, MediaManage, UsersManage, ContentManage, MediaRetry]), StringComparer.Ordinal);

    public static IReadOnlyCollection<string> ExpandLegacy(IEnumerable<string> permissions)
    {
        var result = new HashSet<string>(permissions, StringComparer.Ordinal);
        foreach (var permission in permissions)
        {
            IEnumerable<string> mapped = permission switch
            {
                CatalogManage => ProductsSet.Concat(CategoriesSet).Concat(BrandsSet),
                MediaManage => MediaSet,
                QuotesManage => QuotesSet,
                UsersManage => UsersSet.Concat(RolesSet).Concat(PermissionsSet),
                ContentManage => ContentSet.Concat(ReferencesSet).Concat(ShowroomSet).Concat(BannersSet).Concat(SeoSet),
                _ => [],
            };
            result.UnionWith(mapped);
        }
        return result;
    }

    private static string[] Values(Type type) => type.GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!).ToArray();
    public static readonly IReadOnlyCollection<string> ProductsSet=Values(typeof(Products)), CategoriesSet=Values(typeof(Categories)), BrandsSet=Values(typeof(Brands)), MediaSet=Values(typeof(Media)), QuotesSet=Values(typeof(Quotes)), UsersSet=Values(typeof(Users)), RolesSet=Values(typeof(Roles)), PermissionsSet=Values(typeof(Permissions)), ContentSet=Values(typeof(Content)), ReferencesSet=Values(typeof(References)), ShowroomSet=Values(typeof(Showroom)), BannersSet=Values(typeof(Banners)), SeoSet=Values(typeof(Seo));
}
