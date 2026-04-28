using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.PageObjects.Admin;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests.Admin;

public class AdminReportsStubTests : AuthenticatedTestBase
{
    private const string Password = "Test123!";

    private async Task RegisterAndLoginAsync(string slug, string? role)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"reports_{slug}_{unique}@example.com";
        await RegisterUserAsync(Page, email, Password, "Reports", slug, $"REP-{unique}");
        if (role is not null)
        {
            await AssignRoleAsync(email, role);
        }
        await LoginAsync(Page, email, Password);
    }

    [Test]
    public async Task Admin_GetReports_Renders_EmptyState()
    {
        await RegisterAndLoginAsync("admin", "Admin");
        var reports = new AdminReportsPage(Page);
        var response = await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Status, Is.EqualTo(200));
        await Expect(reports.AdminAreaWrapper).ToBeVisibleAsync();
        // Spec 010 replaced the stub "Reports coming soon" body with the live dashboard;
        // the report sub-tab strip is a stable marker that the dashboard view rendered.
        await Expect(Page.Locator("[data-testid=report-subtabs]")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Reviewer_GetReports_403()
    {
        await RegisterAndLoginAsync("reviewer", "Reviewer");
        var response = await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        Assert.That(response, Is.Not.Null);
        // The new AccessDeniedPath redirects authenticated-but-unauthorized users to
        // /Account/AccessDenied which sets HTTP 403 explicitly.
        Assert.That(response!.Status, Is.EqualTo(403));
    }

    [Test]
    public async Task Applicant_GetReports_403()
    {
        await RegisterAndLoginAsync("applicant", role: null);
        var response = await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Status, Is.EqualTo(403));
    }

    [Test]
    public async Task Unauthenticated_GetReports_RedirectsToLogin()
    {
        var response = await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        Assert.That(response, Is.Not.Null);
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/Login"));
    }
}
