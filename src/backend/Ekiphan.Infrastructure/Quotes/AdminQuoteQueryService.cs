using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Identity;
using Ekiphan.Domain.Quotes;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Quotes;

internal sealed class AdminQuoteQueryService(EkiphanDbContext dbContext)
    : IAdminQuoteQueryService
{
    public async Task<AdminQuotePagedResult<AdminQuoteSummary>> GetQuotesAsync(
        AdminQuoteListQuery request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var search = Normalize(request.Search, 100, nameof(request.Search));
        var productSearch = Normalize(
            request.ProductSearch,
            100,
            nameof(request.ProductSearch));
        var query = dbContext.QuoteRequests.AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(item => item.Status == request.Status.Value);
        }
        if (request.NewOnly)
        {
            query = query.Where(item => item.Status == QuoteStatus.New);
        }
        if (request.DateFrom.HasValue)
        {
            query = query.Where(item => item.CreatedAt >= request.DateFrom.Value);
        }
        if (request.DateTo.HasValue)
        {
            query = query.Where(item => item.CreatedAt <= request.DateTo.Value);
        }
        if (request.UnassignedOnly)
        {
            query = query.Where(item => item.AssignedToUserId == null);
        }
        else if (request.AssignedUserId.HasValue)
        {
            query = query.Where(
                item => item.AssignedToUserId == request.AssignedUserId.Value);
        }
        if (search is not null)
        {
            query = query.Where(item =>
                item.RequestNumber.Contains(search) ||
                item.FullName.Contains(search) ||
                item.CompanyName.Contains(search) ||
                item.Email.Contains(search) ||
                item.Phone.Contains(search));
        }
        if (productSearch is not null)
        {
            query = query.Where(item => item.Items.Any(product =>
                product.SKU.Contains(productSearch) ||
                product.ProductName.Contains(productSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = request.SortOrder == AdminQuoteSortOrder.Oldest
            ? query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
            : query.OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id);

        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new
            {
                item.Id,
                item.RequestNumber,
                item.FullName,
                item.CompanyName,
                item.Email,
                item.Phone,
                item.Country,
                ProductName = item.Items
                    .OrderBy(product => product.CreatedAt)
                    .Select(product => product.ProductName)
                    .FirstOrDefault(),
                SKU = item.Items
                    .OrderBy(product => product.CreatedAt)
                    .Select(product => product.SKU)
                    .FirstOrDefault(),
                item.Status,
                item.AssignedToUserId,
                AssignedToDisplayName = dbContext.AdminUsers
                    .Where(user => user.Id == item.AssignedToUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                item.CreatedAt,
                item.UpdatedAt,
                item.RowVersion,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(item => new AdminQuoteSummary(
            item.Id,
            item.RequestNumber,
            item.FullName,
            item.CompanyName,
            MaskEmail(item.Email),
            MaskPhone(item.Phone),
            item.Country,
            item.ProductName,
            item.SKU,
            item.Status,
            item.AssignedToUserId,
            item.AssignedToDisplayName,
            item.CreatedAt,
            item.UpdatedAt,
            Convert.ToBase64String(item.RowVersion))).ToArray();

        return new AdminQuotePagedResult<AdminQuoteSummary>(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<AdminQuoteDetail?> GetQuoteAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.QuoteRequests
            .AsNoTracking()
            .Where(item => item.Id == quoteId)
            .Select(item => new
            {
                item.Id,
                item.RequestNumber,
                item.FullName,
                item.CompanyName,
                item.Phone,
                item.Email,
                item.Country,
                item.City,
                item.Sector,
                item.ProjectName,
                item.Message,
                item.LanguageCode,
                item.KvkkConsentAt,
                item.KvkkConsentVersion,
                item.CommercialCommunicationConsentAt,
                item.CommercialCommunicationConsentVersion,
                item.Status,
                item.AssignedToUserId,
                AssignedToDisplayName = dbContext.AdminUsers
                    .Where(user => user.Id == item.AssignedToUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                item.CreatedAt,
                item.UpdatedAt,
                item.RowVersion,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (quote is null)
        {
            return null;
        }

        var items = await dbContext.Set<QuoteRequestItem>()
            .AsNoTracking()
            .Where(item => item.QuoteRequestId == quoteId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Select(item => new AdminQuoteItem(
                item.Id,
                item.ProductId,
                item.VariantId,
                item.ProductName,
                item.SKU,
                item.BrandName,
                item.Quantity,
                item.VariantSnapshot,
                item.ProductNote,
                item.ImageStorageKey))
            .ToListAsync(cancellationToken);

        var history = await dbContext.Set<QuoteStatusHistory>()
            .AsNoTracking()
            .Where(item => item.QuoteRequestId == quoteId)
            .OrderBy(item => item.ChangedAt)
            .ThenBy(item => item.Id)
            .Select(item => new AdminQuoteStatusHistoryItem(
                item.Id,
                item.FromStatus,
                item.ToStatus,
                item.ChangedAt,
                item.ChangedByUserId,
                dbContext.AdminUsers
                    .Where(user => user.Id == item.ChangedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                item.Note))
            .ToListAsync(cancellationToken);

        var notes = await dbContext.QuoteInternalNotes
            .AsNoTracking()
            .Where(item => item.QuoteRequestId == quoteId && !item.IsDeleted)
            .OrderByDescending(item => item.RecordedAt)
            .ThenByDescending(item => item.Id)
            .Select(item => new AdminQuoteInternalNoteItem(
                item.Id,
                item.Text,
                item.RecordedAt,
                item.AuthorUserId,
                dbContext.AdminUsers
                    .Where(user => user.Id == item.AuthorUserId)
                    .Select(user => user.DisplayName)
                    .First()))
            .ToListAsync(cancellationToken);

        return new AdminQuoteDetail(
            quote.Id,
            quote.RequestNumber,
            quote.FullName,
            quote.CompanyName,
            quote.Phone,
            quote.Email,
            quote.Country,
            quote.City,
            quote.Sector,
            quote.ProjectName,
            quote.Message,
            quote.LanguageCode,
            quote.KvkkConsentAt,
            quote.KvkkConsentVersion,
            quote.CommercialCommunicationConsentAt,
            quote.CommercialCommunicationConsentVersion,
            quote.Status,
            quote.AssignedToUserId,
            quote.AssignedToDisplayName,
            quote.CreatedAt,
            quote.UpdatedAt,
            Convert.ToBase64String(quote.RowVersion),
            items,
            history,
            notes);
    }

    public async Task<IReadOnlyList<AdminRequestAssignee>> GetAssigneesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AdminUsers
            .AsNoTracking()
            .Where(user =>
                user.IsActive &&
                user.Permissions.Any(grant =>
                    grant.Permission == AdminPermissionCode.QuotesRead ||
                    grant.Permission == AdminPermissionCode.QuotesManage))
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Id)
            .Select(user => new AdminRequestAssignee(
                user.Id,
                user.DisplayName))
            .ToListAsync(cancellationToken);

    private static void Validate(AdminQuoteListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.PageSize, 100);
        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value) ||
            !Enum.IsDefined(query.SortOrder) ||
            query.AssignedUserId.HasValue && query.UnassignedOnly ||
            query.DateFrom.HasValue && query.DateTo.HasValue &&
            query.DateFrom > query.DateTo)
        {
            throw new ArgumentException("Quote filters are invalid.");
        }
    }

    private static string? Normalize(
        string? value,
        int maximum,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= maximum
            ? normalized
            : throw new ArgumentOutOfRangeException(parameterName);
    }

    private static string MaskEmail(string email)
    {
        var separator = email.IndexOf('@');
        if (separator <= 0) return "***";
        var local = email[..separator];
        return $"{local[0]}***{email[separator..]}";
    }

    private static string MaskPhone(string phone)
    {
        var visible = new string(phone.Where(char.IsDigit).TakeLast(4).ToArray());
        return visible.Length == 0 ? "***" : $"*** {visible}";
    }
}
