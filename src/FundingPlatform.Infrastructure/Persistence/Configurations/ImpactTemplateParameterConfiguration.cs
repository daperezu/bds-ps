using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class ImpactTemplateParameterConfiguration : IEntityTypeConfiguration<ImpactTemplateParameter>
{
    public void Configure(EntityTypeBuilder<ImpactTemplateParameter> builder)
    {
        builder.ToTable("ImpactTemplateParameters");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ImpactTemplateId).IsRequired();
        builder.HasIndex(p => p.ImpactTemplateId).HasDatabaseName("IX_ImpactTemplateParams_TemplateId");

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.DisplayLabel).IsRequired().HasMaxLength(300);
        builder.Property(p => p.DataType).IsRequired();
        builder.Property(p => p.IsRequired).IsRequired();
        builder.Property(p => p.ValidationRules);
        builder.Property(p => p.SortOrder).IsRequired();
    }
}
