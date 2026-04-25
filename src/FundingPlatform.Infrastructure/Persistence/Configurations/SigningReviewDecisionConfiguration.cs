using FundingPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FundingPlatform.Infrastructure.Persistence.Configurations;

public class SigningReviewDecisionConfiguration : IEntityTypeConfiguration<SigningReviewDecision>
{
    public void Configure(EntityTypeBuilder<SigningReviewDecision> builder)
    {
        builder.ToTable("SigningReviewDecisions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.SignedUploadId).IsRequired();
        builder.Property(d => d.Outcome).IsRequired().HasConversion<int>();
        builder.Property(d => d.ReviewerUserId).IsRequired().HasMaxLength(450);
        builder.Property(d => d.Comment).HasMaxLength(2000);
        builder.Property(d => d.DecidedAtUtc).IsRequired();

        builder.HasIndex(d => d.SignedUploadId)
            .IsUnique()
            .HasDatabaseName("UQ_SigningReviewDecisions_SignedUploadId");

        builder.HasIndex(d => d.ReviewerUserId)
            .HasDatabaseName("IX_SigningReviewDecisions_ReviewerUserId");
    }
}
