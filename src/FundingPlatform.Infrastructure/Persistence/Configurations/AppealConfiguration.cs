using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class AppealConfiguration : IEntityTypeConfiguration<Appeal>
{
    public void Configure(EntityTypeBuilder<Appeal> builder)
    {
        builder.ToTable("Appeals");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ApplicationId).IsRequired();
        builder.HasIndex(a => a.ApplicationId).HasDatabaseName("IX_Appeals_ApplicationId");

        builder.Property(a => a.ApplicantResponseId).IsRequired();
        builder.Property(a => a.OpenedAt).IsRequired();
        builder.Property(a => a.OpenedByUserId).IsRequired().HasMaxLength(450);

        builder.Property(a => a.Status).IsRequired().HasConversion<int>();
        builder.Property(a => a.Resolution).HasConversion<int?>();
        builder.Property(a => a.ResolvedAt);
        builder.Property(a => a.ResolvedByUserId).HasMaxLength(450);

        builder.Property(a => a.RowVersion).IsRowVersion();

        var messagesNav = builder.Metadata.FindNavigation(nameof(Appeal.Messages))!;
        messagesNav.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(a => a.Messages)
            .WithOne()
            .HasForeignKey(m => m.AppealId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
