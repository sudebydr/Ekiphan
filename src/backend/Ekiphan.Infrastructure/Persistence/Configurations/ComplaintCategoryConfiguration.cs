using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ComplaintCategoryConfiguration : IEntityTypeConfiguration<ComplaintCategory>
{
    public void Configure(EntityTypeBuilder<ComplaintCategory> builder)
    {
        builder.ToTable("ComplaintCategories");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.SortOrder).IsRequired();
        builder.HasIndex(item => new { item.ContactReasonId, item.Name }).IsUnique();
        builder.HasIndex(item => new { item.IsActive, item.SortOrder });
        builder.HasOne<ContactReason>()
            .WithMany()
            .HasForeignKey(item => item.ContactReasonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
