using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ApplicationPage
{
    private readonly IPage _page;

    public ApplicationPage(IPage page)
    {
        _page = page;
    }

    public ILocator CreateButton => _page.Locator("a[href*='Application/Create']");
    public ILocator SubmitDraftButton => _page.Locator("button[type=submit]:has-text('Create Draft Application')");
    public ILocator ApplicationsTable => _page.Locator("table");
    public ILocator AddItemButton => _page.Locator("a:has-text('Add Item')");
    public ILocator SubmitApplicationButton => _page.Locator("button[type=submit]:has-text('Submit Application')");
    public ILocator StatusBadge => _page.Locator(".badge");
    public ILocator ItemRows => _page.Locator("table tbody tr");

    public async Task GotoListAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Application");
    }

    public async Task CreateApplicationAsync()
    {
        await CreateButton.ClickAsync();
        await SubmitDraftButton.ClickAsync();
    }

    public async Task ViewApplicationAsync(int id)
    {
        await _page.Locator($"a[href*='Application/Details/{id}'], a[href*='Application/{id}']").First.ClickAsync();
    }
}
