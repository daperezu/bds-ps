using FundingPlatform.Application;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Application.Options;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Infrastructure;
using FundingPlatform.Infrastructure.DocumentGeneration;
using FundingPlatform.Infrastructure.Identity;
using FundingPlatform.Infrastructure.Persistence;
using FundingPlatform.Web.Identity;
using FundingPlatform.Web.Middleware;
using FundingPlatform.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<AppDbContext>("fundingdb");

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IFundingAgreementHtmlRenderer, RazorFundingAgreementHtmlRenderer>();

// Admin Reports configuration (FR-007: fail fast when DefaultCurrency is missing).
builder.Services.Configure<AdminReportsOptions>(
    builder.Configuration.GetSection(AdminReportsOptions.SectionName));

var adminReportsDefaultCurrency = builder.Configuration[$"{AdminReportsOptions.SectionName}:DefaultCurrency"];
if (string.IsNullOrWhiteSpace(adminReportsDefaultCurrency) || adminReportsDefaultCurrency.Trim().Length != 3)
{
    throw new InvalidOperationException(
        "AdminReports:DefaultCurrency is required and must be a 3-character currency code (e.g., 'COP', 'USD'). Set the configuration value before starting the host.");
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddUserStore<SentinelAwareUserStore>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new AdminAreaViewLocationExpander());
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<SecurityStampValidatorOptions>(o =>
    o.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddScoped<IClaimsTransformation, AdminImpliesReviewerClaimsTransformation>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        scope.ServiceProvider.GetRequiredService<SyncfusionLicenseValidator>().ValidateOrThrow();
    }
    catch (InvalidOperationException ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Syncfusion license validation skipped: {Message}", ex.Message);
    }

    var bootstrapLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var identityLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("FundingPlatform.Infrastructure.Identity.IdentityConfiguration");

    // E2E fixture deploys the dacpac AFTER the web app starts; production deploys
    // it before (Aspire WaitFor). The seed steps below need the schema (and the
    // dacpac post-deploy role rows). Retry SqlException with bounded backoff so
    // both scenarios converge.
    const int maxAttempts = 60;
    var seeded = false;
    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        try
        {
            await FundingPlatform.Infrastructure.Identity.IdentityConfiguration.SeedRolesAsync(scope.ServiceProvider);

            await FundingPlatform.Infrastructure.Identity.IdentityConfiguration.SeedSentinelAdminAsync(
                scope.ServiceProvider, app.Configuration, identityLogger);

            if (app.Environment.IsDevelopment())
            {
                await FundingPlatform.Infrastructure.Identity.IdentityConfiguration.SeedUsersAsync(scope.ServiceProvider);
            }
            seeded = true;
            break;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            if (attempt == maxAttempts - 1)
            {
                bootstrapLogger.LogWarning(
                    "Database schema not ready after {Attempts} attempts; seed skipped. Error: {Message}",
                    maxAttempts, ex.Message);
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
    if (seeded)
    {
        bootstrapLogger.LogInformation("Identity seed completed.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<MustChangePasswordMiddleware>();

app.MapStaticAssets();
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
