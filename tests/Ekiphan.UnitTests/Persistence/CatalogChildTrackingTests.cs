using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.UnitTests.Persistence;

public sealed class CatalogChildTrackingTests
{
    [Fact]
    public void AttributeOptionsAddedToTrackedAttributeAreTrackedAsAdded()
    {
        using var context = CreateContext();
        var attribute = new AttributeDefinition(
            Guid.NewGuid(),
            "COLOR",
            AttributeDataType.Option);

        context.Attach(attribute);
        var option = attribute.AddOption(Guid.NewGuid(), "RED", 1);
        context.ChangeTracker.DetectChanges();

        Assert.Equal(EntityState.Added, context.Entry(option).State);
    }

    private static EkiphanDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<EkiphanDbContext>()
            .UseSqlServer(
                "Server=tracking-only;Database=TrackingOnly;Integrated Security=True;" +
                "TrustServerCertificate=True")
            .Options);
}
