using Ekiphan.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class QuoteStatusHistoryConfiguration
    : IEntityTypeConfiguration<QuoteStatusHistory>
{
    public void Configure(EntityTypeBuilder<QuoteStatusHistory> builder)
    {
        builder.ToTable("QuoteStatusHistory");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();
        builder.Property(history => history.Note).HasMaxLength(1000);
        builder.HasIndex(history => new
        {
            history.QuoteRequestId,
            history.ChangedAt,
        });
    }
}
