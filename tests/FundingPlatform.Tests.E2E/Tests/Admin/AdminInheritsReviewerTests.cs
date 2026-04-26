using FundingPlatform.Tests.E2E.Fixtures;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests.Admin;

public class AdminInheritsReviewerTests : AuthenticatedTestBase
{
    private const string AdminPassword = "Test123!";

    private async Task SignInAsAdminOnlyAsync()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"admin_only_{unique}@example.com";
        await RegisterUserAsync(Page, email, AdminPassword, "AdminOnly", "Inherits", $"AOI-{unique}");
        await AssignRoleAsync(email, "Admin");
        await LoginAsync(Page, email, AdminPassword);
    }

    [Test]
    public async Task Admin_CanAccess_ReviewQueue_NoForbidden()
    {
        await SignInAsAdminOnlyAsync();
        var response = await Page.GotoAsync($"{BaseUrl}/Review");
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Status, Is.LessThan(400),
            "Admin user should reach Reviewer-gated /Review without 403.");
        await Expect(Page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/AccessDenied"));
        await Expect(Page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/Login"));
    }

    [Test]
    public async Task Admin_CanAccess_SigningInbox_NoForbidden()
    {
        await SignInAsAdminOnlyAsync();
        var response = await Page.GotoAsync($"{BaseUrl}/Review/SigningInbox");
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Status, Is.LessThan(400),
            "Admin user should reach Reviewer-gated /Review/SigningInbox without 403.");
    }

    // Note: a meta-test that asserts on the literal Authorize attribute strings was tried
    // and removed — pre-spec-009 commits in main already use Roles="Reviewer,Admin", so a
    // string-match check fires false positives. The two functional tests above are the
    // real contract: an Admin-only user reaches Reviewer-gated routes.
}
