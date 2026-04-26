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

    [Test]
    public async Task NoExistingReviewerGate_HasBeen_BroadenedTo_RolesIncludingAdmin()
    {
        // Meta-test: confirm spec 009 did not modify any [Authorize(Roles="Reviewer")] attribute
        // to add ",Admin" — Reviewer-inheritance is delivered by AdminImpliesReviewerClaimsTransformation,
        // not by attribute edits (FR-002).
        var repoRoot = LocateRepoRoot();
        var controllerPaths = new[]
        {
            Path.Combine(repoRoot, "src/FundingPlatform.Web/Controllers/ReviewController.cs"),
            Path.Combine(repoRoot, "src/FundingPlatform.Web/Controllers/ApplicantResponseController.cs"),
            Path.Combine(repoRoot, "src/FundingPlatform.Web/Controllers/FundingAgreementController.cs"),
        };

        foreach (var path in controllerPaths.Where(File.Exists))
        {
            var contents = await File.ReadAllTextAsync(path);
            Assert.That(contents,
                Does.Not.Contain("Roles=\"Reviewer,Admin\"").And.Not.Contain("Roles = \"Reviewer,Admin\""),
                $"{Path.GetFileName(path)} must not have been broadened to 'Reviewer,Admin'; rely on the claims transformation instead.");
            Assert.That(contents,
                Does.Not.Contain("Roles=\"Admin,Reviewer\"").And.Not.Contain("Roles = \"Admin,Reviewer\""),
                $"{Path.GetFileName(path)} must not have been broadened to 'Admin,Reviewer'; rely on the claims transformation instead.");
        }
    }

    private static string LocateRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FundingPlatform.slnx")))
        {
            dir = dir.Parent;
        }
        Assert.That(dir, Is.Not.Null, "Could not locate repo root from test BaseDirectory.");
        return dir!.FullName;
    }
}
