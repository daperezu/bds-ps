using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class FundingAgreementConfiguration : IEntityTypeConfiguration<FundingAgreement>
{
    public void Configure(EntityTypeBuilder<FundingAgreement> builder)
    {
        builder.ToTable("FundingAgreements");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.ApplicationId).IsRequired();
        builder.HasIndex(f => f.ApplicationId)
            .IsUnique()
            .HasDatabaseName("UQ_FundingAgreements_ApplicationId");

        builder.Property(f => f.FileName).IsRequired().HasMaxLength(260);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(f => f.Size).IsRequired();
        builder.Property(f => f.StoragePath).IsRequired().HasMaxLength(500);
        builder.Property(f => f.GeneratedAtUtc).IsRequired();
        builder.Property(f => f.GeneratedByUserId).IsRequired().HasMaxLength(450);

        builder.Property(f => f.RowVersion).IsRowVersion();

        builder
            .HasOne<AppEntity>()
            .WithOne(a => a.FundingAgreement!)
            .HasForeignKey<FundingAgreement>(f => f.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
