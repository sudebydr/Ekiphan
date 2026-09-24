using Ekiphan.Domain.Identity;
using Ekiphan.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class QuoteInternalNoteConfiguration
    : IEntityTypeConfiguration<QuoteInternalNote>
{
    public void Configure(EntityTypeBuilder<QuoteInternalNote> builder)
    {
        builder.ToTable("QuoteInternalNotes");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Text).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.RecordedAt).HasPrecision(0);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasIndex(item => new { item.QuoteRequestId, item.IsDeleted, item.RecordedAt });
        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(item => item.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
