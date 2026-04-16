using FundingPlatform.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FundingPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ApplicationService>();
        services.AddScoped<AdminService>();
        services.AddScoped<ReviewService>();

        return services;
    }
}
