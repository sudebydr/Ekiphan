using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ContactReasonConfiguration : IEntityTypeConfiguration<ContactReason>
{
    public void Configure(EntityTypeBuilder<ContactReason> builder)
    {
        builder.ToTable("ContactReasons");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.SortOrder).IsRequired();
        builder.HasIndex(item => item.Name).IsUnique();
        builder.HasIndex(item => new { item.IsActive, item.SortOrder });
        builder.HasIndex(item => item.IsComplaintReason)
            .IsUnique()
            .HasFilter("[IsComplaintReason] = 1");
    }
}
