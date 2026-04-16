using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class ImpactParameterValueConfiguration : IEntityTypeConfiguration<ImpactParameterValue>
{
    public void Configure(EntityTypeBuilder<ImpactParameterValue> builder)
    {
        builder.ToTable("ImpactParameterValues");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ImpactId).IsRequired();
        builder.Property(v => v.ImpactTemplateParameterId).IsRequired();
        builder.Property(v => v.Value);

        builder.HasIndex(v => new { v.ImpactId, v.ImpactTemplateParameterId })
            .IsUnique()
            .HasDatabaseName("UX_ImpactParamValues_ImpactId_ParamId");

        builder.HasOne(v => v.ImpactTemplateParameter)
            .WithMany()
            .HasForeignKey(v => v.ImpactTemplateParameterId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
