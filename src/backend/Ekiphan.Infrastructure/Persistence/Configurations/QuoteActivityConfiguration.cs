using Ekiphan.Domain.Identity;
using Ekiphan.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class QuoteActivityConfiguration : IEntityTypeConfiguration<QuoteActivity>
{
    public void Configure(EntityTypeBuilder<QuoteActivity> builder)
    {
        builder.ToTable("QuoteActivities");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.PreviousValue).HasMaxLength(500);
        builder.Property(item => item.NewValue).HasMaxLength(500);
        builder.Property(item => item.Description).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.MetadataJson).HasMaxLength(4000);
        builder.Property(item => item.IpAddress).HasMaxLength(100);
        builder.Property(item => item.UserAgent).HasMaxLength(500);
        builder.Property(item => item.CorrelationId).HasMaxLength(100).IsRequired();

        builder.HasIndex(item => new { item.QuoteRequestId, item.CreatedAt });
        builder.HasIndex(item => item.ActivityType);
        builder.HasIndex(item => item.ActorUserId);
        builder.HasIndex(item => item.CorrelationId);

        builder.HasOne<QuoteRequest>()
            .WithMany()
            .HasForeignKey(item => item.QuoteRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(item => item.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
