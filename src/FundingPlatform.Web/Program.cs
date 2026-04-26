using FundingPlatform.Application;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Infrastructure;
using FundingPlatform.Infrastructure.DocumentGeneration;
using FundingPlatform.Infrastructure.Identity;
using FundingPlatform.Infrastructure.Persistence;
using FundingPlatform.Web.Middleware;
using FundingPlatform.Web.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<AppDbContext>("fundingdb");

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IFundingAgreementHtmlRenderer, RazorFundingAgreementHtmlRenderer>();

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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<SecurityStampValidatorOptions>(o =>
    o.ValidationInterval = TimeSpan.FromMinutes(1));

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

    try
    {
        await FundingPlatform.Infrastructure.Identity.IdentityConfiguration.SeedRolesAsync(scope.ServiceProvider);

        var identityLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("FundingPlatform.Infrastructure.Identity.IdentityConfiguration");
        await FundingPlatform.Infrastructure.Identity.IdentityConfiguration.SeedSentinelAdminAsync(
            scope.ServiceProvider, app.Configuration, identityLogger);

        if (app.Environment.IsDevelopment())
        {
            await FundingPlatform.Infrastructure.Identity.IdentityConfiguration.SeedUsersAsync(scope.ServiceProvider);
        }
    }
    catch (Microsoft.Data.SqlClient.SqlException ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Database schema not ready — deploy the dacpac and restart. Error: {Message}", ex.Message);
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
