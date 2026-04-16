using FundingPlatform.Domain.Entities;
using FundingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FundingPlatform.Infrastructure.Identity;

public static class IdentityConfiguration
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = ["Applicant", "Admin", "Reviewer"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        var seedUsers = new[]
        {
            new { Email = "applicant@demo.com", Password = "Demo123!", FirstName = "Ana", LastName = "Pérez", LegalId = "DEMO-APP-001", Roles = new[] { "Applicant" } },
            new { Email = "reviewer@demo.com", Password = "Demo123!", FirstName = "Carlos", LastName = "Rivera", LegalId = "DEMO-REV-001", Roles = new[] { "Reviewer" } },
            new { Email = "admin@demo.com", Password = "Demo123!", FirstName = "María", LastName = "Torres", LegalId = "DEMO-ADM-001", Roles = new[] { "Admin" } },
        };

        foreach (var seed in seedUsers)
        {
            if (await userManager.FindByEmailAsync(seed.Email) is not null)
                continue;

            var user = new IdentityUser { UserName = seed.Email, Email = seed.Email };
            var result = await userManager.CreateAsync(user, seed.Password);
            if (!result.Succeeded)
                continue;

            foreach (var role in seed.Roles)
            {
                await userManager.AddToRoleAsync(user, role);
            }

            // Create Applicant record so the user can interact with the system
            if (!await dbContext.Applicants.AnyAsync(a => a.UserId == user.Id))
            {
                dbContext.Applicants.Add(new Applicant(
                    userId: user.Id,
                    legalId: seed.LegalId,
                    firstName: seed.FirstName,
                    lastName: seed.LastName,
                    email: seed.Email,
                    phone: null,
                    performanceScore: null));
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
