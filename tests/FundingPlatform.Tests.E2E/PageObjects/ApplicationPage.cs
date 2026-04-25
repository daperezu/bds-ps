using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ApplicationPage : BasePage
{
    public ApplicationPage(IPage page) : base(page)
    {
    }

    public ILocator CreateButton => Page.Locator("a[href*='Application/Create']");
    public ILocator SubmitDraftButton => Page.Locator("button[type=submit]:has-text('Create Draft Application')");
    public ILocator ApplicationsTable => Page.Locator("table");
    public ILocator AddItemButton => Page.Locator("a:has-text('Add Item')");
    public ILocator SubmitApplicationButton => Page.Locator("button[type=submit]:has-text('Submit Application')");
    public ILocator StatusBadge => Page.Locator(".badge");
    public ILocator ItemRows => Page.Locator("table tbody tr");

    public async Task GotoListAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Application");
    }

    public async Task CreateApplicationAsync()
    {
        await CreateButton.ClickAsync();
        await SubmitDraftButton.ClickAsync();
    }

    public async Task ViewApplicationAsync(int id)
    {
        await Page.Locator($"a[href*='Application/Details/{id}'], a[href*='Application/{id}']").First.ClickAsync();
    }
}
