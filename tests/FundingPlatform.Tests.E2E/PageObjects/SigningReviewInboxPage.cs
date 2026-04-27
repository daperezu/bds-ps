using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class SigningReviewInboxPage : BasePage
{
    public SigningReviewInboxPage(IPage page) : base(page)
    {
    }

    public ILocator Table => Page.Locator("[data-testid=signing-inbox-table]");
    public ILocator Rows => Page.Locator("[data-testid=signing-inbox-row]");
    public ILocator EmptyState => Page.Locator("[data-testid=signing-inbox-empty]");

    public async Task NavigateAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Review/SigningInbox");
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
