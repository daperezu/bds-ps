using System.Text.RegularExpressions;
using FundingPlatform.Tests.E2E.Constants;
using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.PageObjects;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests;

public class DraftPersistenceTests : AuthenticatedTestBase
{
    [Test]
    public async Task SaveDraft_And_ReturnLater()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"draft_persist_{uniqueId}@example.com";
        var password = "Test123!";

        // Register and login
        await RegisterUserAsync(Page, email, password, "Draft", "Tester", $"LID-{uniqueId}");
        await LoginAsync(Page, email, password);

        // Create application
        var appPage = new ApplicationPage(Page);
        await appPage.GotoListAsync(BaseUrl);
        await appPage.CreateApplicationAsync();

        var url = Page.Url;
        var appIdMatch = Regex.Match(url, @"/Application/Details/(\d+)");
        Assert.That(appIdMatch.Success, Is.True, "Should be on application details page with ID");
        var appId = int.Parse(appIdMatch.Groups[1].Value);

        // Verify Draft status
        var draftBadge = Page.Locator("[data-testid=status-pill]:has-text('Borrador')");
        await Expect(draftBadge).ToBeVisibleAsync();

        // Add an item
        var itemPage = new ItemPage(Page);
        await itemPage.AddItemAsync(appId, "Persisted Laptop", 0, "16GB RAM, 512GB SSD", BaseUrl);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/Details/\d+"));

        // Verify item appears
        var itemRow = Page.Locator("table tbody tr:has-text('Persisted Laptop')");
        await Expect(itemRow).ToBeVisibleAsync();

        // Log out by clicking the logout button in the navigation
        var logoutButton = Page.Locator($"button:has-text('{UiCopy.Logout}')");
        await logoutButton.ClickAsync();
        // Wait for redirect to home
        await Expect(Page).ToHaveURLAsync(new Regex(@"/$|/Home"));

        // Log back in
        await LoginAsync(Page, email, password);

        // Navigate to the application
        await appPage.GotoListAsync(BaseUrl);

        // Verify application appears in the list
        var appRow = Page.Locator($"table tbody tr:has(a[href*='Application/Details/{appId}'])");
        await Expect(appRow).ToBeVisibleAsync();

        // Verify Draft status in the list
        var draftBadgeInList = appRow.Locator("[data-testid=status-pill]:has-text('Borrador')");
        await Expect(draftBadgeInList).ToBeVisibleAsync();

        // Click to view details
        await appPage.ViewApplicationAsync(appId);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/Details/\d+"));

        // Verify the item data is still intact
        var persistedItem = Page.Locator("table tbody tr:has-text('Persisted Laptop')");
        await Expect(persistedItem).ToBeVisibleAsync();

        // Verify application is still in Draft state
        var draftBadgeAfterReturn = Page.Locator("[data-testid=status-pill]:has-text('Borrador')");
        await Expect(draftBadgeAfterReturn).ToBeVisibleAsync();
    }
}
