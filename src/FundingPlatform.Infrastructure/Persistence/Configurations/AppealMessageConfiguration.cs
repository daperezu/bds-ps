using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class AppealMessageConfiguration : IEntityTypeConfiguration<AppealMessage>
{
    public void Configure(EntityTypeBuilder<AppealMessage> builder)
    {
        builder.ToTable("AppealMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.AppealId).IsRequired();
        builder.Property(m => m.AuthorUserId).IsRequired().HasMaxLength(450);
        builder.Property(m => m.Text).IsRequired().HasMaxLength(4000);
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasIndex(m => new { m.AppealId, m.CreatedAt })
            .HasDatabaseName("IX_AppealMessages_AppealId_CreatedAt");
    }
}
