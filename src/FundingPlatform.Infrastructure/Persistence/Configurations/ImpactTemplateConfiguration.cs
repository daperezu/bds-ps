using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class ImpactTemplateConfiguration : IEntityTypeConfiguration<ImpactTemplate>
{
    public void Configure(EntityTypeBuilder<ImpactTemplate> builder)
    {
        builder.ToTable("ImpactTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(300);
        builder.HasIndex(t => t.Name).IsUnique();

        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasMany(t => t.Parameters)
            .WithOne(p => p.ImpactTemplate)
            .HasForeignKey(p => p.ImpactTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
