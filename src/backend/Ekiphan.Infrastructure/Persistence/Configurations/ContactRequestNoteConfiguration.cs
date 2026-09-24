using Ekiphan.Domain.Content;
using Ekiphan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ContactRequestNoteConfiguration
    : IEntityTypeConfiguration<ContactRequestNote>
{
    public void Configure(EntityTypeBuilder<ContactRequestNote> builder)
    {
        builder.ToTable("ContactRequestNotes");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Text).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.RecordedAt).HasPrecision(0);
        builder.HasIndex(item => new { item.ContactRequestId, item.RecordedAt });
        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(item => item.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
