using Ekiphan.Domain.Catalog;
using Ekiphan.Domain.Quotes;

namespace Ekiphan.Application.Quotes;

public sealed class QuoteSubmissionService(
    IQuoteSubmissionRepository repository,
    IQuoteRequestNumberGenerator requestNumberGenerator,
    IBotVerificationService botVerificationService,
    IQuoteSpamDetectionService spamDetectionService,
    IQuoteNotificationService notificationService,
    IQuoteActivityService activityService,
    TimeProvider timeProvider)
{
    public const int MaximumItemCount = 50;

    public async Task<SubmitQuoteResult> SubmitAsync(
        SubmitQuoteCommand command,
        string kvkkConsentVersion,
        string commercialCommunicationConsentVersion,
        QuoteRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var ctx = context ?? new QuoteRequestContext(null, null, Guid.NewGuid().ToString("N"));

        // 1. Honeypot check
        if (!string.IsNullOrWhiteSpace(command.Website))
        {
            throw new QuoteSubmissionException("Quote request could not be submitted.");
        }

        if (!command.KvkkConsent)
        {
            throw new QuoteSubmissionException("KVKK consent is required.");
        }

        // 2. Bot protection check
        var botResult = await botVerificationService.VerifyAsync(ctx.BotToken, ctx.IpAddress, cancellationToken);
        if (!botResult.IsSuccess)
        {
            throw new QuoteSubmissionException($"Bot verification failed: {botResult.Message}");
        }

        // 3. Anti-spam check
        var spamResult = await spamDetectionService.CheckAsync(command, ctx, cancellationToken);
        if (spamResult.RecommendedAction == SpamRecommendedAction.Reject)
        {
            throw new QuoteSubmissionException("Quote request rejected by automated anti-spam policy.");
        }

        if (command.Items is null ||
            command.Items.Count is < 1 or > MaximumItemCount)
        {
            throw new QuoteSubmissionException(
                $"A quote request must contain between 1 and {MaximumItemCount} items.");
        }

        var language = command.LanguageCode?.Trim().ToLowerInvariant();
        if (!LanguageCodes.IsSupported(language ?? string.Empty))
        {
            throw new QuoteSubmissionException("Language must be tr or en.");
        }

        if (command.Items.Any(item =>
                item.Quantity is < 1 or > 100_000 ||
                (item.ProductId.HasValue && item.ProductId.Value == Guid.Empty) ||
                (!item.ProductId.HasValue &&
                 (string.IsNullOrWhiteSpace(item.ProductName) ||
                  string.IsNullOrWhiteSpace(item.Sku)))))
        {
            throw new QuoteSubmissionException(
                "Each quote item must contain a valid product and quantity.");
        }

        var duplicateItem = command.Items
            .Where(item => item.ProductId.HasValue)
            .GroupBy(item => new
            {
                item.ProductId,
                item.VariantId,
            })
            .Any(group => group.Count() > 1);
        if (duplicateItem)
        {
            throw new QuoteSubmissionException(
                "The same product and variant cannot be submitted twice.");
        }

        var persistedProductIds = command.Items
            .Where(item => item.ProductId.HasValue)
            .Select(item => item.ProductId!.Value)
            .Distinct()
            .ToArray();

        var snapshots = await repository.GetProductSnapshotsAsync(
            persistedProductIds,
            language!,
            cancellationToken);
        if (snapshots.Count != persistedProductIds.Length)
        {
            throw new QuoteSubmissionException(
                "One or more products are unavailable.");
        }

        var now = timeProvider.GetUtcNow();
        var quote = CreateQuote(
            command,
            language!,
            kvkkConsentVersion,
            commercialCommunicationConsentVersion,
            now);

        quote.SetSpamAssessment(spamResult.RiskScore, spamResult.RecommendedAction.ToString());

        foreach (var requestedItem in command.Items)
        {
            QuoteProductSnapshot? product = null;
            if (requestedItem.ProductId.HasValue &&
                !snapshots.TryGetValue(requestedItem.ProductId.Value, out product))
            {
                throw new QuoteSubmissionException("One or more products are unavailable.");
            }

            QuoteVariantSnapshot? variant = null;
            if (requestedItem.VariantId.HasValue &&
                (product is null || !product.Variants.TryGetValue(
                    requestedItem.VariantId.Value,
                    out variant)))
            {
                throw new QuoteSubmissionException(
                    "A selected product variant is unavailable.");
            }

            quote.AddItem(
                Guid.NewGuid(),
                product?.ProductId,
                variant?.VariantId,
                product?.ProductName ?? requestedItem.ProductName!,
                variant?.SKU ?? product?.SKU ?? requestedItem.Sku!,
                product?.BrandName ?? requestedItem.Brand,
                requestedItem.Quantity,
                variant?.Description,
                requestedItem.Note,
                product?.ImageStorageKey);
        }

        quote.ValidateForSubmission();
        repository.Add(quote);
        await repository.SaveChangesAsync(cancellationToken);

        // 4. Log activity & queue notification outbox
        await activityService.LogAsync(
            quote.Id,
            QuoteActivityType.Created,
            null,
            null,
            quote.RequestNumber,
            $"Public quote request created ({quote.RequestNumber}).",
            new { SpamScore = spamResult.RiskScore, SpamAction = spamResult.RecommendedAction.ToString() },
            ctx,
            cancellationToken);

        await notificationService.QueueNewQuoteNotificationsAsync(quote.Id, ctx, cancellationToken);

        return new SubmitQuoteResult(quote.RequestNumber, now);
    }

    private QuoteRequest CreateQuote(
        SubmitQuoteCommand command,
        string language,
        string kvkkConsentVersion,
        string commercialCommunicationConsentVersion,
        DateTimeOffset now)
    {
        try
        {
            return new QuoteRequest(
                Guid.NewGuid(),
                requestNumberGenerator.Generate(now),
                command.FullName,
                command.CompanyName,
                command.Phone,
                command.Email,
                command.Country,
                command.City,
                command.Sector,
                command.ProjectName,
                command.Message,
                language,
                now,
                kvkkConsentVersion,
                command.CommercialCommunicationConsent ? now : null,
                command.CommercialCommunicationConsent
                    ? commercialCommunicationConsentVersion
                    : null);
        }
        catch (ArgumentException exception)
        {
            throw new QuoteSubmissionException(exception.Message);
        }
    }
}
