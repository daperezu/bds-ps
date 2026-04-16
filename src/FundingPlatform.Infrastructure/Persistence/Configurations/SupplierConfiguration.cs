using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.LegalId).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.LegalId).IsUnique().HasDatabaseName("UX_Suppliers_LegalId");

        builder.Property(s => s.Name).IsRequired().HasMaxLength(300);
        builder.Property(s => s.ContactName).HasMaxLength(200);
        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.Location).HasMaxLength(500);
        builder.Property(s => s.HasElectronicInvoice).IsRequired();
        builder.Property(s => s.ShippingDetails).HasMaxLength(500);
        builder.Property(s => s.WarrantyInfo).HasMaxLength(500);
        builder.Property(s => s.IsCompliantCCSS).IsRequired();
        builder.Property(s => s.IsCompliantHacienda).IsRequired();
        builder.Property(s => s.IsCompliantSICOP).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
