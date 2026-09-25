using System.Security.Cryptography;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Domain.Identity;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

internal sealed class ContactRequestService(
    EkiphanDbContext dbContext,
    TimeProvider timeProvider) : IContactRequestService
{
    public async Task<ContactSubmissionResult> SubmitAsync(
        SubmitContactCommand command,
        string consentVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.KvkkConsent)
        {
            throw new ArgumentException("KVKK consent is required.");
        }
        if (!string.IsNullOrWhiteSpace(command.Website))
        {
            throw new ArgumentException("Contact request could not be accepted.");
        }

        ContactReason? reason = null;
        if (command.ContactReasonId.HasValue)
        {
            reason = await dbContext.ContactReasons.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == command.ContactReasonId && item.IsActive,
                    cancellationToken) ?? throw new ArgumentException(
                    "The selected contact reason is invalid.");
        }
        if (reason?.IsComplaintReason == true)
        {
            if (!command.ComplaintCategoryId.HasValue ||
                !await dbContext.ComplaintCategories.AsNoTracking().AnyAsync(
                    item => item.Id == command.ComplaintCategoryId &&
                        item.ContactReasonId == reason.Id && item.IsActive,
                    cancellationToken))
            {
                throw new ArgumentException(
                    "An active complaint category is required.");
            }
        }
        else if (command.ComplaintCategoryId.HasValue)
        {
            throw new ArgumentException(
                "Complaint category is only valid for customer complaints.");
        }

        var now = timeProvider.GetUtcNow();
        var request = new ContactRequest(
            Guid.NewGuid(), command.FullName, command.Email, command.Phone,
            command.CompanyName, command.Subject, command.Message,
            command.LanguageCode, now, consentVersion,
            command.ContactReasonId, command.ComplaintCategoryId);
        dbContext.ContactRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ContactSubmissionResult(request.Id, now);
    }

    public async Task<AdminContactPage> GetAdminAsync(
        AdminContactListQuery request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var search = Normalize(request.Search, 100);
        var query = dbContext.ContactRequests.AsNoTracking();
        if (request.Status.HasValue)
        {
            query = query.Where(item => item.Status == request.Status.Value);
        }
        if (request.NewOnly)
        {
            query = query.Where(item => item.Status == ContactRequestStatus.New);
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
            query = query.Where(item =>
                item.AssignedToUserId == request.AssignedUserId.Value);
        }
        if (request.ContactReasonId.HasValue)
        {
            query = query.Where(item =>
                item.ContactReasonId == request.ContactReasonId.Value);
        }
        if (search is not null)
        {
            query = query.Where(item =>
                item.FullName.Contains(search) ||
                item.Subject.Contains(search) ||
                item.Email.Contains(search) ||
                item.CompanyName != null && item.CompanyName.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        query = request.SortOrder == AdminContactSortOrder.Oldest
            ? query.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
            : query.OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id);
        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new
            {
                item.Id,
                item.CreatedAt,
                item.FullName,
                item.Email,
                item.Phone,
                item.Subject,
                item.Message,
                item.Status,
                ReasonName = dbContext.ContactReasons
                    .Where(value => value.Id == item.ContactReasonId)
                    .Select(value => value.Name).FirstOrDefault(),
                ComplaintCategoryName = dbContext.ComplaintCategories
                    .Where(value => value.Id == item.ComplaintCategoryId)
                    .Select(value => value.Name).FirstOrDefault(),
                item.AssignedToUserId,
                AssignedToDisplayName = dbContext.AdminUsers
                    .Where(user => user.Id == item.AssignedToUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                item.UpdatedAt,
                item.RowVersion,
            })
            .ToListAsync(cancellationToken);

        return new AdminContactPage(
            rows.Select(item => new AdminContactSummary(
                item.Id,
                item.CreatedAt,
                item.FullName,
                MaskEmail(item.Email),
                item.Phone is null ? null : MaskPhone(item.Phone),
                item.Subject,
                Preview(item.Message),
                item.Status,
                item.ReasonName,
                item.ComplaintCategoryName,
                item.AssignedToUserId,
                item.AssignedToDisplayName,
                item.UpdatedAt,
                Convert.ToBase64String(item.RowVersion))).ToArray(),
            request.Page,
            request.PageSize,
            total);
    }

    public async Task<AdminContactDetail?> GetAdminDetailAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await dbContext.ContactRequests.AsNoTracking()
            .Where(item => item.Id == requestId)
            .Select(item => new
            {
                item.Id,
                item.FullName,
                item.Email,
                item.Phone,
                item.CompanyName,
                item.Subject,
                item.Message,
                item.LanguageCode,
                item.ConsentAt,
                item.ConsentVersion,
                ReasonName = dbContext.ContactReasons
                    .Where(value => value.Id == item.ContactReasonId)
                    .Select(value => value.Name).FirstOrDefault(),
                ComplaintCategoryName = dbContext.ComplaintCategories
                    .Where(value => value.Id == item.ComplaintCategoryId)
                    .Select(value => value.Name).FirstOrDefault(),
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
        if (request is null) return null;

        var history = await dbContext.Set<ContactRequestStatusHistory>()
            .AsNoTracking()
            .Where(item => item.ContactRequestId == requestId)
            .OrderBy(item => item.ChangedAt)
            .ThenBy(item => item.Id)
            .Select(item => new AdminContactStatusHistoryItem(
                item.Id,
                item.FromStatus,
                item.ToStatus,
                item.ChangedByUserId,
                dbContext.AdminUsers
                    .Where(user => user.Id == item.ChangedByUserId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                item.ChangedAt))
            .ToListAsync(cancellationToken);
        var notes = await dbContext.ContactRequestNotes.AsNoTracking()
            .Where(item => item.ContactRequestId == requestId)
            .OrderByDescending(item => item.RecordedAt)
            .ThenByDescending(item => item.Id)
            .Select(item => new AdminContactNoteItem(
                item.Id,
                item.Text,
                item.AuthorUserId,
                dbContext.AdminUsers
                    .Where(user => user.Id == item.AuthorUserId)
                    .Select(user => user.DisplayName)
                    .First(),
                item.RecordedAt))
            .ToListAsync(cancellationToken);

        return new AdminContactDetail(
            request.Id, request.FullName, request.Email, request.Phone,
            request.CompanyName, request.Subject, request.Message,
            request.LanguageCode, request.ConsentAt, request.ConsentVersion,
            request.Status, request.AssignedToUserId,
            request.AssignedToDisplayName, request.CreatedAt, request.UpdatedAt,
            Convert.ToBase64String(request.RowVersion), request.ReasonName,
            request.ComplaintCategoryName, history, notes);
    }

    public async Task<AdminComplaintPage> GetComplaintsAsync(
        AdminComplaintListQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100 ||
            request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
        {
            throw new ArgumentException("Complaint filters are invalid.");
        }
        var search = Normalize(request.Search, 100);
        var query = dbContext.ContactRequests.AsNoTracking()
            .Where(item => item.ContactReasonId != null &&
                dbContext.ContactReasons.Any(reason =>
                    reason.Id == item.ContactReasonId && reason.IsComplaintReason));
        if (request.Status.HasValue)
        {
            query = query.Where(item => item.Status == request.Status.Value);
        }
        if (search is not null)
        {
            query = query.Where(item => item.FullName.Contains(search) ||
                item.Email.Contains(search) || item.Subject.Contains(search) ||
                item.CompanyName != null && item.CompanyName.Contains(search));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new AdminComplaintSummary(
                item.Id, item.FullName, item.Email, item.Phone, item.CompanyName,
                dbContext.ComplaintCategories
                    .Where(category => category.Id == item.ComplaintCategoryId)
                    .Select(category => category.Name).FirstOrDefault() ?? "—",
                item.Subject, item.CreatedAt, item.Status, item.AssignedToUserId,
                dbContext.AdminUsers.Where(user => user.Id == item.AssignedToUserId)
                    .Select(user => user.DisplayName).FirstOrDefault()))
            .ToListAsync(cancellationToken);
        return new AdminComplaintPage(items, request.Page, request.PageSize, total);
    }

    public async Task<IReadOnlyList<AdminContactAssignee>> GetAssigneesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AdminUsers.AsNoTracking()
            .Where(user =>
                user.IsActive &&
                user.Permissions.Any(grant =>
                    grant.Permission == AdminPermissionCode.ContactsRead ||
                    grant.Permission == AdminPermissionCode.ContactsManage))
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Id)
            .Select(user => new AdminContactAssignee(user.Id, user.DisplayName))
            .ToListAsync(cancellationToken);

    public async Task<AdminContactMutationResult> ChangeStatusAsync(
        ChangeContactStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateMutation(
            command.ContactRequestId,
            command.ChangedByUserId,
            command.ExpectedVersion);
        if (!Enum.IsDefined(command.Status))
        {
            throw new ArgumentOutOfRangeException(nameof(command));
        }
        var request = await GetCurrentAsync(
            command.ContactRequestId,
            command.ExpectedVersion,
            cancellationToken);
        request.TransitionTo(
            command.Status,
            command.ChangedByUserId,
            timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return Result(request);
    }

    public async Task<AdminContactMutationResult> AssignAsync(
        AssignContactCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateMutation(
            command.ContactRequestId,
            command.ChangedByUserId,
            command.ExpectedVersion);
        if (command.AssignedToUserId.HasValue &&
            !await CanAssignAsync(command.AssignedToUserId.Value, cancellationToken))
        {
            throw new ArgumentException(
                "Assigned user is not an active contact operator.");
        }
        var request = await GetCurrentAsync(
            command.ContactRequestId,
            command.ExpectedVersion,
            cancellationToken);
        request.AssignTo(command.AssignedToUserId);
        request.AddInternalNote(
            command.ChangedByUserId,
            command.AssignedToUserId.HasValue
                ? "Contact request assigned to an operator."
                : "Contact request assignment removed.",
            timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return Result(request);
    }

    public async Task<AdminContactMutationResult> AddNoteAsync(
        AddContactNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateMutation(
            command.ContactRequestId,
            command.AuthorUserId,
            command.ExpectedVersion);
        var request = await GetCurrentAsync(
            command.ContactRequestId,
            command.ExpectedVersion,
            cancellationToken);
        request.AddInternalNote(
            command.AuthorUserId,
            command.Text,
            timeProvider.GetUtcNow());
        await SaveAsync(cancellationToken);
        return Result(request);
    }

    private async Task<ContactRequest> GetCurrentAsync(
        Guid id,
        byte[] expectedVersion,
        CancellationToken cancellationToken)
    {
        var request = await dbContext.ContactRequests
            .Include(item => item.StatusHistory)
            .Include(item => item.InternalNotes)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null) throw new ContactRequestNotFoundException(id);
        if (request.RowVersion.Length != expectedVersion.Length ||
            !CryptographicOperations.FixedTimeEquals(
                request.RowVersion,
                expectedVersion))
        {
            throw new ContactRequestConcurrencyException();
        }
        return request;
    }

    private Task<bool> CanAssignAsync(Guid userId, CancellationToken token) =>
        dbContext.AdminUsers.AnyAsync(user =>
            user.Id == userId && user.IsActive &&
            user.Permissions.Any(grant =>
                grant.Permission == AdminPermissionCode.ContactsRead ||
                grant.Permission == AdminPermissionCode.ContactsManage), token);

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContactRequestConcurrencyException();
        }
    }

    private static AdminContactMutationResult Result(ContactRequest request) =>
        new(request.Id, request.Status, request.AssignedToUserId,
            request.UpdatedAt, Convert.ToBase64String(request.RowVersion));

    private static void Validate(AdminContactListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Page < 1 || query.PageSize is < 1 or > 100 ||
            query.Status.HasValue && !Enum.IsDefined(query.Status.Value) ||
            !Enum.IsDefined(query.SortOrder) ||
            query.AssignedUserId.HasValue && query.UnassignedOnly ||
            query.DateFrom.HasValue && query.DateTo.HasValue &&
            query.DateFrom > query.DateTo)
        {
            throw new ArgumentException("Contact filters are invalid.");
        }
    }

    private static void ValidateMutation(Guid id, Guid actor, byte[] version)
    {
        if (id == Guid.Empty || actor == Guid.Empty || version is not { Length: 8 })
        {
            throw new ArgumentException("Identifiers and row version are required.");
        }
    }

    private static string? Normalize(string? value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= maximum
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static string Preview(string value) =>
        value.Length <= 120 ? value : string.Concat(value.AsSpan(0, 117), "...");

    private static string MaskEmail(string email)
    {
        var separator = email.IndexOf('@');
        return separator <= 0 ? "***" : $"{email[0]}***{email[separator..]}";
    }

    private static string MaskPhone(string phone)
    {
        var visible = new string(phone.Where(char.IsDigit).TakeLast(4).ToArray());
        return visible.Length == 0 ? "***" : $"*** {visible}";
    }
}