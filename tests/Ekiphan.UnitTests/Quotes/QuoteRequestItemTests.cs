using Ekiphan.Domain.Quotes;

namespace Ekiphan.UnitTests.Quotes;

public sealed class QuoteRequestItemTests
{
    [Fact]
    public void QuantityMustBePositive()
    {
        var request = CreateRequest();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => request.AddItem(
                Guid.NewGuid(),
                null,
                null,
                "Archived Product",
                "SKU-1",
                null,
                0));
    }

    [Fact]
    public void ImageStorageKeyRejectsTraversal()
    {
        var request = CreateRequest();

        Assert.Throws<ArgumentException>(
            () => request.AddItem(
                Guid.NewGuid(),
                null,
                null,
                "Archived Product",
                "SKU-1",
                null,
                1,
                imageStorageKey: "../private/image.webp"));
    }

    private static QuoteRequest CreateRequest() =>
        new(
            Guid.NewGuid(),
            "Q-2026-0002",
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
            "kvkk-v1");
}
