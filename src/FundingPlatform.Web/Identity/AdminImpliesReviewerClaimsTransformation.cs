using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace FundingPlatform.Web.Identity;

public class AdminImpliesReviewerClaimsTransformation : IClaimsTransformation
{
    private const string AdminRole = "Admin";
    private const string ReviewerRole = "Reviewer";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (!principal.IsInRole(AdminRole)) return Task.FromResult(principal);
        if (principal.IsInRole(ReviewerRole)) return Task.FromResult(principal);

        if (principal.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, ReviewerRole));
        }
        return Task.FromResult(principal);
    }
}
