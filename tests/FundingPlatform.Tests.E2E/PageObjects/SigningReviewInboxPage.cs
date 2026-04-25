using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class SigningReviewInboxPage
{
    private readonly IPage _page;

    public SigningReviewInboxPage(IPage page)
    {
        _page = page;
    }

    public ILocator Table => _page.Locator("[data-testid=signing-inbox-table]");
    public ILocator Rows => _page.Locator("[data-testid=signing-inbox-row]");
    public ILocator EmptyState => _page.Locator("[data-testid=signing-inbox-empty]");

    public async Task NavigateAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Review/SigningInbox");
    }

    public async Task<int> RowCount()
    {
        return await Rows.CountAsync();
    }

    public async Task ClickFirstRow()
    {
        await Rows.First.Locator("a").ClickAsync();
    }
}
