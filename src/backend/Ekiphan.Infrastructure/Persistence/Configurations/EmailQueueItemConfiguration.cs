using Ekiphan.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class EmailQueueItemConfiguration : IEntityTypeConfiguration<EmailQueueItem>
{
    public void Configure(EntityTypeBuilder<EmailQueueItem> builder)
    {
        builder.ToTable("EmailQueueItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.RelatedEntityType).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Recipient).HasMaxLength(254).IsRequired();
        builder.Property(item => item.Subject).HasMaxLength(300).IsRequired();
        builder.Property(item => item.TemplateCode).HasMaxLength(100).IsRequired();
        builder.Property(item => item.TemplateDataJson).HasMaxLength(8000).IsRequired();
        builder.Property(item => item.LastErrorCode).HasMaxLength(100);
        builder.Property(item => item.LastErrorMessage).HasMaxLength(1000);
        builder.Property(item => item.CorrelationId).HasMaxLength(100).IsRequired();

        builder.HasIndex(item => new { item.Status, item.NextAttemptAt });
        builder.HasIndex(item => new { item.RelatedEntityType, item.RelatedEntityId });
        builder.HasIndex(item => item.Recipient);
        builder.HasIndex(item => item.CreatedAt);
    }
}
