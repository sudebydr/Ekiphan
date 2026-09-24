using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Quotes;

public sealed class QuoteRequest : Entity
{
    private static readonly Dictionary<QuoteStatus, QuoteStatus[]> Transitions =
        new Dictionary<QuoteStatus, QuoteStatus[]>
        {
            [QuoteStatus.New] = [QuoteStatus.Reviewing, QuoteStatus.Archived],
            [QuoteStatus.Reviewing] =
                [QuoteStatus.Contacted, QuoteStatus.Preparing, QuoteStatus.Lost, QuoteStatus.Archived],
            [QuoteStatus.Contacted] =
                [QuoteStatus.Preparing, QuoteStatus.Lost, QuoteStatus.Archived],
            [QuoteStatus.Preparing] =
                [QuoteStatus.Sent, QuoteStatus.Lost, QuoteStatus.Archived],
            [QuoteStatus.Sent] =
                [QuoteStatus.Won, QuoteStatus.Lost, QuoteStatus.Archived],
            [QuoteStatus.Won] = [QuoteStatus.Archived],
            [QuoteStatus.Lost] = [QuoteStatus.Archived],
            [QuoteStatus.Archived] = [],
        };

    private readonly List<QuoteRequestItem> _items = [];
    private readonly List<QuoteStatusHistory> _statusHistory = [];
    private readonly List<QuoteRequestAttachment> _attachments = [];
    private readonly List<QuoteInternalNote> _internalNotes = [];

    private QuoteRequest()
    {
    }

    public QuoteRequest(
        Guid id,
        string requestNumber,
        string fullName,
        string companyName,
        string phone,
        string email,
        string country,
        string? city,
        string? sector,
        string? projectName,
        string? message,
        string languageCode,
        DateTimeOffset kvkkConsentAt,
        string kvkkConsentVersion,
        DateTimeOffset? commercialCommunicationConsentAt = null,
        string? commercialCommunicationConsentVersion = null)
        : base(id)
    {
        var normalizedLanguage = languageCode?.Trim().ToLowerInvariant();

        if (!LanguageCodes.IsSupported(normalizedLanguage ?? string.Empty))
        {
            throw new ArgumentOutOfRangeException(
                nameof(languageCode),
                languageCode,
                "Only Turkish and English are supported.");
        }

        if (kvkkConsentAt == default)
        {
            throw new ArgumentException(
                "KVKK consent timestamp is required.",
                nameof(kvkkConsentAt));
        }

        if (commercialCommunicationConsentAt.HasValue !=
            !string.IsNullOrWhiteSpace(commercialCommunicationConsentVersion))
        {
            throw new ArgumentException(
                "Commercial communication consent timestamp and version must be supplied together.",
                nameof(commercialCommunicationConsentVersion));
        }

        RequestNumber = QuoteGuard.Required(requestNumber, 30, nameof(requestNumber))
            .ToUpperInvariant();
        FullName = QuoteGuard.Required(fullName, 200, nameof(fullName));
        CompanyName = QuoteGuard.Required(companyName, 200, nameof(companyName));
        Phone = QuoteGuard.Required(phone, 50, nameof(phone));
        Email = QuoteGuard.Email(email);
        Country = QuoteGuard.Required(country, 100, nameof(country));
        City = QuoteGuard.Optional(city, 100, nameof(city));
        Sector = QuoteGuard.Optional(sector, 150, nameof(sector));
        ProjectName = QuoteGuard.Optional(projectName, 200, nameof(projectName));
        Message = QuoteGuard.Optional(message, 4000, nameof(message));
        LanguageCode = normalizedLanguage!;
        KvkkConsentAt = kvkkConsentAt;
        KvkkConsentVersion = QuoteGuard.Required(
            kvkkConsentVersion,
            100,
            nameof(kvkkConsentVersion));
        CommercialCommunicationConsentAt = commercialCommunicationConsentAt;
        CommercialCommunicationConsentVersion = QuoteGuard.Optional(
            commercialCommunicationConsentVersion,
            100,
            nameof(commercialCommunicationConsentVersion));
        LastActivityAt = kvkkConsentAt;
        _statusHistory.Add(
            new QuoteStatusHistory(
                Guid.NewGuid(),
                Id,
                null,
                QuoteStatus.New,
                kvkkConsentAt,
                null,
                "Quote request created."));
    }

    public string RequestNumber { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public string? City { get; private set; }

    public string? Sector { get; private set; }

    public string? ProjectName { get; private set; }

    public string? Message { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public DateTimeOffset KvkkConsentAt { get; private set; }

    public string KvkkConsentVersion { get; private set; } = string.Empty;

    public DateTimeOffset? CommercialCommunicationConsentAt { get; private set; }

    public string? CommercialCommunicationConsentVersion { get; private set; }

    public QuoteStatus Status { get; private set; } = QuoteStatus.New;

    public Guid? AssignedToUserId { get; private set; }

    public DateTimeOffset LastActivityAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public Guid? ArchivedByUserId { get; private set; }

    public string? ArchiveReason { get; private set; }

    public bool IsAnonymized { get; private set; }

    public DateTimeOffset? AnonymizedAt { get; private set; }

    public int SpamRiskScore { get; private set; }

    public string? SpamStatus { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<QuoteRequestItem> Items => _items;

    public IReadOnlyCollection<QuoteStatusHistory> StatusHistory => _statusHistory;

    public IReadOnlyCollection<QuoteRequestAttachment> Attachments => _attachments;

    public IReadOnlyCollection<QuoteInternalNote> InternalNotes => _internalNotes;

    public QuoteRequestItem AddItem(
        Guid itemId,
        Guid? productId,
        Guid? variantId,
        string productName,
        string sku,
        string? brandName,
        int quantity,
        string? variantSnapshot = null,
        string? productNote = null,
        string? imageStorageKey = null)
    {
        if (_items.Any(
                item =>
                    item.ProductId == productId &&
                    item.VariantId == variantId &&
                    item.SKU.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "The same product and variant already exists in the quote.");
        }

        var item = new QuoteRequestItem(
            itemId,
            Id,
            productId,
            variantId,
            productName,
            sku,
            brandName,
            quantity,
            variantSnapshot,
            productNote,
            imageStorageKey);
        _items.Add(item);
        return item;
    }

    public void AddAttachment(Guid mediaAssetId)
    {
        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Media asset identifier cannot be empty.",
                nameof(mediaAssetId));
        }

        if (_attachments.Any(item => item.MediaAssetId == mediaAssetId))
        {
            throw new InvalidOperationException(
                "The media asset is already attached to this quote.");
        }

        _attachments.Add(new QuoteRequestAttachment(Id, mediaAssetId));
    }

    public void ValidateForSubmission()
    {
        if (_items.Count == 0)
        {
            throw new InvalidOperationException(
                "A quote request must contain at least one item.");
        }
    }

    public void TransitionTo(
        QuoteStatus newStatus,
        DateTimeOffset changedAt,
        Guid? changedByUserId,
        string? note = null)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        }

        if (!Transitions[Status].Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"Transition from '{Status}' to '{newStatus}' is not allowed.");
        }

        var previousStatus = Status;
        Status = newStatus;
        _statusHistory.Add(
            new QuoteStatusHistory(
                Guid.NewGuid(),
                Id,
                previousStatus,
                newStatus,
                changedAt,
                changedByUserId,
                note));
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

    public QuoteInternalNote AddInternalNote(
        Guid authorUserId,
        string text,
        DateTimeOffset createdAt)
    {
        var note = new QuoteInternalNote(
            Guid.NewGuid(),
            Id,
            authorUserId,
            text,
            createdAt);
        _internalNotes.Add(note);
        TouchActivity(createdAt);
        return note;
    }

    public void TouchActivity(DateTimeOffset activityAt)
    {
        LastActivityAt = activityAt != default ? activityAt : DateTimeOffset.UtcNow;
    }

    public void SetSpamAssessment(int riskScore, string spamStatus)
    {
        SpamRiskScore = Math.Clamp(riskScore, 0, 100);
        SpamStatus = QuoteGuard.Optional(spamStatus, 50, nameof(spamStatus));
    }

    public void Archive(Guid? archivedByUserId, string reason, DateTimeOffset archivedAt)
    {
        if (Status == QuoteStatus.Archived)
        {
            throw new InvalidOperationException("Quote is already archived.");
        }

        Status = QuoteStatus.Archived;
        ArchivedByUserId = archivedByUserId;
        ArchiveReason = QuoteGuard.Required(reason, 500, nameof(reason));
        ArchivedAt = archivedAt != default ? archivedAt : DateTimeOffset.UtcNow;
        TouchActivity(ArchivedAt.Value);
    }

    public void Anonymize(DateTimeOffset anonymizedAt)
    {
        if (IsAnonymized)
        {
            return;
        }

        FullName = "Anonim Kullanıcı";
        Email = $"anonymized-{Id:N}@anonymized.local";
        Phone = "0000000000";
        City = null;
        Sector = null;
        ProjectName = null;
        Message = "[ANONİMLEŞTİRİLDİ]";
        IsAnonymized = true;
        AnonymizedAt = anonymizedAt != default ? anonymizedAt : DateTimeOffset.UtcNow;
        TouchActivity(AnonymizedAt.Value);
    }
}
