using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class VersionHistoryConfiguration : IEntityTypeConfiguration<VersionHistory>
{
    public void Configure(EntityTypeBuilder<VersionHistory> builder)
    {
        builder.ToTable("VersionHistory");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ApplicationId).IsRequired();
        builder.HasIndex(v => v.ApplicationId).HasDatabaseName("IX_VersionHistory_ApplicationId");

        builder.Property(v => v.UserId).IsRequired().HasMaxLength(450);
        builder.Property(v => v.Action).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Details);
        builder.Property(v => v.Timestamp).IsRequired();
    }
}
