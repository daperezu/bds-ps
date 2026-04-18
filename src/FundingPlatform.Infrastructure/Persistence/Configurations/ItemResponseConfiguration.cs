using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class ItemResponseConfiguration : IEntityTypeConfiguration<ItemResponse>
{
    public void Configure(EntityTypeBuilder<ItemResponse> builder)
    {
        builder.ToTable("ItemResponses");

        builder.HasKey(ir => ir.Id);

        builder.Property(ir => ir.ApplicantResponseId).IsRequired();
        builder.Property(ir => ir.ItemId).IsRequired();
        builder.Property(ir => ir.Decision).IsRequired().HasConversion<int>();

        builder.HasIndex(ir => ir.ItemId).HasDatabaseName("IX_ItemResponses_ItemId");
        builder.HasIndex(ir => new { ir.ApplicantResponseId, ir.ItemId })
            .IsUnique()
            .HasDatabaseName("UQ_ItemResponses_ResponseItem");
    }
}
