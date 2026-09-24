using Ekiphan.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class QuoteSpamAssessmentConfiguration : IEntityTypeConfiguration<QuoteSpamAssessment>
{
    public void Configure(EntityTypeBuilder<QuoteSpamAssessment> builder)
    {
        builder.ToTable("QuoteSpamAssessments");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.ReasonsJson).HasMaxLength(2000).IsRequired();

        builder.HasIndex(item => item.QuoteRequestId);
        builder.HasIndex(item => item.RiskScore);

        builder.HasOne<QuoteRequest>()
            .WithMany()
            .HasForeignKey(item => item.QuoteRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
