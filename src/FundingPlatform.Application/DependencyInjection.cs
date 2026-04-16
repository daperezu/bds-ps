using FundingPlatform.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FundingPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ApplicationService>();
        services.AddScoped<AdminService>();

        return services;
    }
}
