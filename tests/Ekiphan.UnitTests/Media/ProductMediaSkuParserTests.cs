using Ekiphan.Application.MediaImport;
using Ekiphan.Infrastructure.MediaImport;

namespace Ekiphan.UnitTests.Media;

public sealed class ProductMediaSkuParserTests
{
    [Theory]
    [InlineData("3121.KTH.PAN26CK855A14-2.webp", "3121.KTH.PAN26CK855A14", 2, false)]
    [InlineData("3110.KTH.BNSMP29CK00-1.webp", "3110.KTH.BNSMP29CK00", 1, true)]
    public void NumericDashSuffixIsRemovedFromSku(
        string fileName,
        string expectedSku,
        int expectedSortOrder,
        bool expectedPrimary)
    {
        var result = new ProductMediaSkuParser().Parse(fileName);

        Assert.Equal(expectedSku, result.Sku);
        Assert.Equal(ProductMediaDetectedPosition.Gallery, result.Position);
        Assert.Equal(expectedSortOrder, result.SuggestedSortOrder);
        Assert.Equal(expectedPrimary, result.SuggestedIsPrimary);
    }
}
