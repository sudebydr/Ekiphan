using Ekiphan.Domain.Quotes;

namespace Ekiphan.UnitTests.Quotes;

public sealed class QuoteRequestTests
{
    [Fact]
    public void KvkkConsentTimestampIsRequired()
    {
        Assert.Throws<ArgumentException>(
            () => new QuoteRequest(
                Guid.NewGuid(),
                "Q-1",
                "Test User",
                "Test Company",
                "+90 555 000 00 00",
                "test@example.com",
                "Türkiye",
                null,
                null,
                null,
                null,
                "tr",
                default,
                "kvkk-v1"));
    }

    [Fact]
    public void CommercialConsentIsStoredSeparatelyFromKvkkConsent()
    {
        var kvkkConsentAt = DateTimeOffset.UtcNow;
        var request = CreateRequest(kvkkConsentAt: kvkkConsentAt);

        Assert.Equal(kvkkConsentAt, request.KvkkConsentAt);
        Assert.Null(request.CommercialCommunicationConsentAt);
    }

    [Fact]
    public void CommercialConsentTimestampAndVersionMustBeSuppliedTogether()
    {
        Assert.Throws<ArgumentException>(
            () => new QuoteRequest(
                Guid.NewGuid(),
                "Q-1",
                "Test User",
                "Test Company",
                "+90 555 000 00 00",
                "test@example.com",
                "Türkiye",
                null,
                null,
                null,
                null,
                "tr",
                DateTimeOffset.UtcNow,
                "kvkk-v1",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void InvalidEmailIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new QuoteRequest(
                Guid.NewGuid(),
                "Q-1",
                "Test User",
                "Test Company",
                "+90 555 000 00 00",
                "not-an-email",
                "Türkiye",
                null,
                null,
                null,
                null,
                "tr",
                DateTimeOffset.UtcNow,
                "kvkk-v1"));
    }

    [Fact]
    public void QuoteMustContainAtLeastOneItemBeforeSubmission()
    {
        var request = CreateRequest();

        Assert.Throws<InvalidOperationException>(request.ValidateForSubmission);
    }

    [Fact]
    public void ProductSnapshotIsPreservedOnItem()
    {
        var request = CreateRequest();

        var item = request.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Polikarbon Bardak",
            "sku-1",
            "Rubikap",
            12,
            "Renk: Şeffaf",
            "Proje için",
            "products/sku-1.webp");

        Assert.Equal("Polikarbon Bardak", item.ProductName);
        Assert.Equal("SKU-1", item.SKU);
        Assert.Equal("Rubikap", item.BrandName);
        Assert.Equal(12, item.Quantity);
        request.ValidateForSubmission();
    }

    [Fact]
    public void DuplicateProductVariantCannotBeAdded()
    {
        var request = CreateRequest();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        request.AddItem(
            Guid.NewGuid(),
            productId,
            variantId,
            "Product",
            "SKU-1",
            null,
            1);

        Assert.Throws<InvalidOperationException>(
            () => request.AddItem(
                Guid.NewGuid(),
                productId,
                variantId,
                "Product",
                "sku-1",
                null,
                1));
    }

    [Fact]
    public void NewQuoteCanMoveToReviewing()
    {
        var request = CreateRequest();
        var changedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        request.TransitionTo(
            QuoteStatus.Reviewing,
            changedAt,
            Guid.NewGuid(),
            "Assigned.");

        Assert.Equal(QuoteStatus.Reviewing, request.Status);
        Assert.Equal(2, request.StatusHistory.Count);
        Assert.Equal(QuoteStatus.New, request.StatusHistory.Last().FromStatus);
        Assert.Equal(QuoteStatus.Reviewing, request.StatusHistory.Last().ToStatus);
    }

    [Fact]
    public void NewQuoteCannotMoveDirectlyToWon()
    {
        var request = CreateRequest();

        Assert.Throws<InvalidOperationException>(
            () => request.TransitionTo(
                QuoteStatus.Won,
                DateTimeOffset.UtcNow,
                Guid.NewGuid()));
    }

    [Fact]
    public void DuplicateAttachmentIsRejected()
    {
        var request = CreateRequest();
        var mediaAssetId = Guid.NewGuid();
        request.AddAttachment(mediaAssetId);

        Assert.Throws<InvalidOperationException>(
            () => request.AddAttachment(mediaAssetId));
    }

    private static QuoteRequest CreateRequest(
        DateTimeOffset? kvkkConsentAt = null) =>
        new(
            Guid.NewGuid(),
            "Q-2026-0001",
            "Test User",
            "Test Company",
            "+90 555 000 00 00",
            "test@example.com",
            "Türkiye",
            "İstanbul",
            "Otel",
            "Test Project",
            "Please contact us.",
            "tr",
            kvkkConsentAt ?? DateTimeOffset.UtcNow,
            "kvkk-v1");
}
