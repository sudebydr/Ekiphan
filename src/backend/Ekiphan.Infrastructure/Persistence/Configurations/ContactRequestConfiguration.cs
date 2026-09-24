using Ekiphan.Domain.Content;
using Ekiphan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ContactRequestConfiguration : IEntityTypeConfiguration<ContactRequest>
{
    public void Configure(EntityTypeBuilder<ContactRequest> builder)
    {
        builder.ToTable("ContactRequests");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.FullName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Email).HasMaxLength(320).IsUnicode(false).IsRequired();
        builder.Property(item => item.Phone).HasMaxLength(50).IsUnicode(false);
        builder.Property(item => item.CompanyName).HasMaxLength(200);
        builder.Property(item => item.Subject).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Message).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.LanguageCode).HasMaxLength(2).IsUnicode(false);
        builder.Property(item => item.ConsentAt).HasPrecision(0);
        builder.Property(item => item.ConsentVersion).HasMaxLength(100).IsUnicode(false);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.Property(item => item.CreatedAt).HasPrecision(0);
        builder.Property(item => item.UpdatedAt).HasPrecision(0);
        builder.HasIndex(item => item.CreatedAt);
        builder.HasIndex(item => item.Email);
        builder.HasIndex(item => new { item.Status, item.CreatedAt });
        builder.HasIndex(item => item.AssignedToUserId);
        builder.HasIndex(item => new { item.ContactReasonId, item.CreatedAt });
        builder.HasIndex(item => item.ComplaintCategoryId);
        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(item => item.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<ContactReason>()
            .WithMany()
            .HasForeignKey(item => item.ContactReasonId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ComplaintCategory>()
            .WithMany()
            .HasForeignKey(item => item.ComplaintCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.StatusHistory)
            .WithOne()
            .HasForeignKey(item => item.ContactRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.StatusHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(item => item.InternalNotes)
            .WithOne()
            .HasForeignKey(item => item.ContactRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.InternalNotes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
