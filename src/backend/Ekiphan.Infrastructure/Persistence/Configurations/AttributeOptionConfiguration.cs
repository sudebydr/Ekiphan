using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class AttributeOptionConfiguration
    : IEntityTypeConfiguration<AttributeOption>
{
    public void Configure(EntityTypeBuilder<AttributeOption> builder)
    {
        builder.ToTable("AttributeOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Id).ValueGeneratedNever();
        builder.Property(option => option.Code)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(option => new
        {
            option.AttributeId,
            option.Code,
        }).IsUnique();
        builder.HasMany(option => option.Translations)
            .WithOne()
            .HasForeignKey(translation => translation.AttributeOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
