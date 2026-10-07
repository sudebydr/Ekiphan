using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.MediaImport;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.UnitTests.Media;

public sealed class ProductMediaImportDuplicateRulesTests
{
    private const string Hash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public void SameBinaryForDifferentProductsRemainsValidAndCanReuseExistingAsset()
    {
        var firstProduct = Guid.NewGuid();
        var secondProduct = Guid.NewGuid();
        var asset = Guid.NewGuid();
        var files = new List<ProductMediaImportFileValidationDto>
        {
            File(firstProduct), File(secondProduct)
        };

        ProductMediaImportValidationService.ApplyContentDuplicateRules(files,
            new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase) { [Hash] = asset }, EmptyLinks(), true);

        Assert.All(files, file =>
        {
            Assert.Equal(ProductMediaValidationFileStatus.Valid, file.Status);
            Assert.Equal(asset, file.ExistingMediaAssetId);
        });
    }

    [Fact]
    public void SameBinaryForSameProductIsSkippedAfterFirstAssignment()
    {
        var product = Guid.NewGuid();
        var files = new List<ProductMediaImportFileValidationDto> { File(product), File(product) };

        ProductMediaImportValidationService.ApplyContentDuplicateRules(files, EmptyAssets(), EmptyLinks(), true);

        Assert.Equal(ProductMediaValidationFileStatus.Valid, files[0].Status);
        Assert.Equal(ProductMediaValidationFileStatus.Duplicate, files[1].Status);
        Assert.Contains(files[1].Warnings, x => x.Code == "DUPLICATE_MEDIA");
    }

    [Fact]
    public void DifferentBinariesForSameProductRemainValid()
    {
        var product = Guid.NewGuid();
        var otherHash = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        var files = new List<ProductMediaImportFileValidationDto>
        {
            File(product), File(product) with { ContentHash = otherHash }
        };

        ProductMediaImportValidationService.ApplyContentDuplicateRules(files, EmptyAssets(), EmptyLinks(), true);

        Assert.All(files, file => Assert.Equal(ProductMediaValidationFileStatus.Valid, file.Status));
    }

    [Fact]
    public async Task DuplicateAssetHashesUseOneDeterministicReusableAsset()
    {
        await using var db = new EkiphanDbContext(new DbContextOptionsBuilder<EkiphanDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        db.MediaAssets.AddRange(CreateAsset(firstId, "one.webp"), CreateAsset(secondId, "two.webp"));
        await db.SaveChangesAsync();

        var matches = await new ProductMediaImportRepository(db).FindMediaAssetsByHashesAsync([Hash]);

        Assert.Single(matches);
        Assert.Equal(firstId, matches[Hash]);
    }

    [Fact]
    public void MissingExistingAssetDoesNotBecomeAnEmptyAssetIdentifier()
    {
        var files = new List<ProductMediaImportFileValidationDto> { File(Guid.NewGuid()) };

        ProductMediaImportValidationService.ApplyContentDuplicateRules(files, EmptyAssets(), EmptyLinks(), true);

        Assert.Null(files[0].ExistingMediaAssetId);
    }

    [Fact]
    public void ExistingLinkForSameProductIsSkipped()
    {
        var product = Guid.NewGuid();
        var files = new List<ProductMediaImportFileValidationDto> { File(product) };

        ProductMediaImportValidationService.ApplyContentDuplicateRules(files, EmptyAssets(),
            new HashSet<ProductMediaContentLink> { new(product, Hash) }, true);

        Assert.Equal(ProductMediaValidationFileStatus.Duplicate, files[0].Status);
    }

    [Theory]
    [InlineData(true, "PRODUCT_NOT_FOUND")]
    [InlineData(false, "SKU_RESOLUTION_FAILED")]
    public void ResolutionFailureUsesStableReasonCode(bool hasCandidate, string expectedCode)
    {
        var error = ProductMediaImportValidationService.CreateResolutionFailure(hasCandidate);

        Assert.Equal(expectedCode, error.Code);
    }

    [Theory]
    [InlineData(ProductMediaValidationFileStatus.Unmatched)]
    [InlineData(ProductMediaValidationFileStatus.Duplicate)]
    [InlineData(ProductMediaValidationFileStatus.Invalid)]
    [InlineData(ProductMediaValidationFileStatus.Conflict)]
    [InlineData(ProductMediaValidationFileStatus.Ignored)]
    public void EntryLevelValidationStatusesAreSkippedByExecutionPolicy(ProductMediaValidationFileStatus status)
    {
        var file = File(Guid.NewGuid()) with { Status = status };

        Assert.False(ProductMediaImportExecutionPolicy.IsImportable(file));
    }

    [Fact]
    public void ValidMatchedEntryIsImportableBySharedExecutionPolicy()
    {
        Assert.True(ProductMediaImportExecutionPolicy.IsImportable(File(Guid.NewGuid())));
    }

    private static ProductMediaImportFileValidationDto File(Guid productId) => new(
        Guid.NewGuid().ToString("N"), "SKU.webp", "SKU", productId, "SKU", "SKU",
        ProductMediaMatchSource.FileName, 1, false, Hash, ProductMediaValidationFileStatus.Valid, [], []);

    private static Dictionary<string, Guid> EmptyAssets() => new();
    private static HashSet<ProductMediaContentLink> EmptyLinks() => [];

    private static MediaAsset CreateAsset(Guid id, string fileName) =>
        MediaAsset.CreateFile(id, MediaAssetType.Image, fileName, $"products/{fileName}", "image/webp", 1, Hash);
}
