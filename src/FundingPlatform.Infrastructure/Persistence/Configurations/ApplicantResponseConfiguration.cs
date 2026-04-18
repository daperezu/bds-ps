using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class ApplicantResponseConfiguration : IEntityTypeConfiguration<ApplicantResponse>
{
    public void Configure(EntityTypeBuilder<ApplicantResponse> builder)
    {
        builder.ToTable("ApplicantResponses");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ApplicationId).IsRequired();
        builder.HasIndex(r => r.ApplicationId).HasDatabaseName("IX_ApplicantResponses_ApplicationId");

        builder.Property(r => r.CycleNumber).IsRequired();
        builder.Property(r => r.SubmittedAt).IsRequired();
        builder.Property(r => r.SubmittedByUserId).IsRequired().HasMaxLength(450);

        builder.HasIndex(r => new { r.ApplicationId, r.CycleNumber })
            .IsUnique()
            .HasDatabaseName("UQ_ApplicantResponses_AppCycle");

        var itemResponsesNav = builder.Metadata.FindNavigation(nameof(ApplicantResponse.ItemResponses))!;
        itemResponsesNav.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.ItemResponses)
            .WithOne()
            .HasForeignKey(ir => ir.ApplicantResponseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
