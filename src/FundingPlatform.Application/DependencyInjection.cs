using FundingPlatform.Application.Options;
using FundingPlatform.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddScoped<ApplicationService>();
        services.AddScoped<AdminService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<ApplicantResponseService>();
        services.AddScoped<FundingAgreementService>();
        services.AddScoped<SignedUploadService>();

        if (configuration is not null)
        {
            services.Configure<SignedUploadOptions>(
                configuration.GetSection(SignedUploadOptions.SectionName));
        }

        return services;
    }
}
