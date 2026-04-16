using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class ImpactConfiguration : IEntityTypeConfiguration<Impact>
{
    public void Configure(EntityTypeBuilder<Impact> builder)
    {
        builder.ToTable("Impacts");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ItemId).IsRequired();
        builder.HasIndex(i => i.ItemId).IsUnique().HasDatabaseName("UX_Impacts_ItemId");

        builder.Property(i => i.ImpactTemplateId).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        builder.HasOne(i => i.ImpactTemplate)
            .WithMany()
            .HasForeignKey(i => i.ImpactTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.ParameterValues)
            .WithOne(pv => pv.Impact)
            .HasForeignKey(pv => pv.ImpactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
