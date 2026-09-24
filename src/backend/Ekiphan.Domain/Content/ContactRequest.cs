using System.Net.Mail;
using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class ContactRequest : Entity
{
    private static readonly Dictionary<
        ContactRequestStatus,
        ContactRequestStatus[]> Transitions = new()
    {
        [ContactRequestStatus.New] =
            [ContactRequestStatus.Read, ContactRequestStatus.Archived],
        [ContactRequestStatus.Read] =
            [ContactRequestStatus.AwaitingResponse, ContactRequestStatus.Completed,
                ContactRequestStatus.Archived],
        [ContactRequestStatus.AwaitingResponse] =
            [ContactRequestStatus.Read, ContactRequestStatus.Completed,
                ContactRequestStatus.Archived],
        [ContactRequestStatus.Completed] = [ContactRequestStatus.Archived],
        [ContactRequestStatus.Archived] = [],
    };

    private readonly List<ContactRequestStatusHistory> _statusHistory = [];
    private readonly List<ContactRequestNote> _internalNotes = [];

    private ContactRequest()
    {
    }

    public ContactRequest(
        Guid id,
        string fullName,
        string email,
        string? phone,
        string? companyName,
        string subject,
        string message,
        string languageCode,
        DateTimeOffset consentAt,
        string consentVersion,
        Guid? contactReasonId = null,
        Guid? complaintCategoryId = null) : base(id)
    {
        FullName = Required(fullName, 200, nameof(fullName));
        Email = ValidEmail(email);
        Phone = Optional(phone, 50, nameof(phone));
        CompanyName = Optional(companyName, 200, nameof(companyName));
        Subject = Required(subject, 200, nameof(subject));
        Message = Required(message, 4000, nameof(message));
        LanguageCode = languageCode.Trim().ToLowerInvariant();
        if (LanguageCode is not ("tr" or "en"))
        {
            throw new ArgumentOutOfRangeException(nameof(languageCode));
        }
        if (consentAt == default)
        {
            throw new ArgumentException("Consent timestamp is required.", nameof(consentAt));
        }
        ConsentAt = consentAt;
        ConsentVersion = Required(consentVersion, 100, nameof(consentVersion));
        ContactReasonId = contactReasonId;
        ComplaintCategoryId = complaintCategoryId;
        _statusHistory.Add(new ContactRequestStatusHistory(
            Guid.NewGuid(),
            Id,
            null,
            ContactRequestStatus.New,
            null,
            consentAt));
    }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? CompanyName { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = string.Empty;
    public DateTimeOffset ConsentAt { get; private set; }
    public string ConsentVersion { get; private set; } = string.Empty;
    public Guid? ContactReasonId { get; private set; }
    public Guid? ComplaintCategoryId { get; private set; }
    public ContactRequestStatus Status { get; private set; } = ContactRequestStatus.New;
    public Guid? AssignedToUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<ContactRequestStatusHistory> StatusHistory =>
        _statusHistory;
    public IReadOnlyCollection<ContactRequestNote> InternalNotes => _internalNotes;

    public void TransitionTo(
        ContactRequestStatus status,
        Guid changedByUserId,
        DateTimeOffset changedAt)
    {
        if (!Enum.IsDefined(status) || !Transitions[Status].Contains(status))
        {
            throw new InvalidOperationException(
                $"Transition from '{Status}' to '{status}' is not allowed.");
        }
        if (changedByUserId == Guid.Empty || changedAt == default)
        {
            throw new ArgumentException("Actor and timestamp are required.");
        }

        var previous = Status;
        Status = status;
        _statusHistory.Add(new ContactRequestStatusHistory(
            Guid.NewGuid(), Id, previous, status, changedByUserId, changedAt));
    }

    public void AssignTo(Guid? assignedToUserId)
    {
        if (assignedToUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Assigned user identifier cannot be empty.",
                nameof(assignedToUserId));
        }
        AssignedToUserId = assignedToUserId;
    }

    public ContactRequestNote AddInternalNote(
        Guid authorUserId,
        string text,
        DateTimeOffset recordedAt)
    {
        var note = new ContactRequestNote(
            Guid.NewGuid(), Id, authorUserId, text, recordedAt);
        _internalNotes.Add(note);
        return note;
    }

    private static string Required(string value, int maximum, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= maximum ? normalized : throw new ArgumentOutOfRangeException(name);
    }

    private static string? Optional(string? value, int maximum, string name) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, maximum, name);

    private static string ValidEmail(string value)
    {
        var email = Required(value, 320, nameof(value));
        try
        {
            return new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase)
                ? email.ToLowerInvariant()
                : throw new ArgumentException("Email address is invalid.", nameof(value));
        }
        catch (FormatException)
        {
            throw new ArgumentException("Email address is invalid.", nameof(value));
        }
    }
}
