using Ekiphan.Domain.Quotes;
using Ekiphan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class QuoteRequestConfiguration
    : IEntityTypeConfiguration<QuoteRequest>
{
    public void Configure(EntityTypeBuilder<QuoteRequest> builder)
    {
        builder.ToTable("QuoteRequests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.RequestNumber)
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(request => request.FullName).HasMaxLength(200).IsRequired();
        builder.Property(request => request.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(request => request.Phone)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(request => request.Email)
            .HasMaxLength(254)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(request => request.Country).HasMaxLength(100).IsRequired();
        builder.Property(request => request.City).HasMaxLength(100);
        builder.Property(request => request.Sector).HasMaxLength(150);
        builder.Property(request => request.ProjectName).HasMaxLength(200);
        builder.Property(request => request.Message).HasMaxLength(4000);
        builder.Property(request => request.LanguageCode)
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(request => request.KvkkConsentVersion)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(request => request.CommercialCommunicationConsentVersion)
            .HasMaxLength(100)
            .IsUnicode(false);
        builder.Property(request => request.ArchiveReason).HasMaxLength(500);
        builder.Property(request => request.SpamStatus).HasMaxLength(50);
        builder.Property(request => request.RowVersion)
            .IsRowVersion();
        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(request => request.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(request => request.RequestNumber).IsUnique();
        builder.HasIndex(request => new
        {
            request.Status,
            request.CreatedAt,
        });
        builder.HasIndex(request => new
        {
            request.Email,
            request.CreatedAt,
        });
        builder.HasIndex(request => request.AssignedToUserId);
        builder.HasIndex(request => request.LastActivityAt);
        builder.HasIndex(request => request.ArchivedAt);
        builder.HasIndex(request => request.IsAnonymized);
        builder.HasMany(request => request.Items)
            .WithOne()
            .HasForeignKey(item => item.QuoteRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(request => request.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.QuoteRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(request => request.Attachments)
            .WithOne()
            .HasForeignKey(attachment => attachment.QuoteRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(request => request.InternalNotes)
            .WithOne()
            .HasForeignKey(note => note.QuoteRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(request => request.InternalNotes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_QuoteRequests_KvkkConsentVersion",
                "LEN([KvkkConsentVersion]) > 0");
            table.HasCheckConstraint(
                "CK_QuoteRequests_CommercialConsentPair",
                "([CommercialCommunicationConsentAt] IS NULL AND " +
                "[CommercialCommunicationConsentVersion] IS NULL) OR " +
                "([CommercialCommunicationConsentAt] IS NOT NULL AND " +
                "[CommercialCommunicationConsentVersion] IS NOT NULL)");
        });
    }
}
