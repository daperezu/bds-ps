using Microsoft.AspNetCore.Identity;

namespace FundingPlatform.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsSystemSentinel { get; init; }
    public bool MustChangePassword { get; set; }

    public ApplicationUser()
    {
    }

    public ApplicationUser(string email, string firstName, string lastName, string? phone)
    {
        UserName = email;
        Email = email;
        NormalizedUserName = email.ToUpperInvariant();
        NormalizedEmail = email.ToUpperInvariant();
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phone;
    }

    public static ApplicationUser CreateSentinel(string email)
    {
        return new ApplicationUser
        {
            UserName = email,
            Email = email,
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            IsSystemSentinel = true,
            MustChangePassword = false,
        };
    }
}
