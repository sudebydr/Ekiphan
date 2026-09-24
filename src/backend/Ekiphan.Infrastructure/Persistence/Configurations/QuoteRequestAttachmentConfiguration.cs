using Ekiphan.Domain.Media;
using Ekiphan.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class QuoteRequestAttachmentConfiguration
    : IEntityTypeConfiguration<QuoteRequestAttachment>
{
    public void Configure(EntityTypeBuilder<QuoteRequestAttachment> builder)
    {
        builder.ToTable("QuoteRequestAttachments");
        builder.HasKey(attachment => new
        {
            attachment.QuoteRequestId,
            attachment.MediaAssetId,
        });
        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(attachment => attachment.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(attachment => attachment.MediaAssetId);
    }
}
