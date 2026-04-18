using FundingPlatform.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FundingPlatform.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<AppEntity> Applications => Set<AppEntity>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ImpactTemplate> ImpactTemplates => Set<ImpactTemplate>();
    public DbSet<ImpactTemplateParameter> ImpactTemplateParameters => Set<ImpactTemplateParameter>();
    public DbSet<Impact> Impacts => Set<Impact>();
    public DbSet<ImpactParameterValue> ImpactParameterValues => Set<ImpactParameterValue>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<VersionHistory> VersionHistories => Set<VersionHistory>();
    public DbSet<ApplicantResponse> ApplicantResponses => Set<ApplicantResponse>();
    public DbSet<Appeal> Appeals => Set<Appeal>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
