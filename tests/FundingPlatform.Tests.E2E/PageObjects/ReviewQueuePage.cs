using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ReviewQueuePage
{
    private readonly IPage _page;

    public ReviewQueuePage(IPage page)
    {
        _page = page;
    }

    public ILocator QueueTable => _page.Locator("table.review-queue");
    public ILocator QueueRows => _page.Locator("table.review-queue tbody tr");
    public ILocator PaginationLinks => _page.Locator("nav.pagination a");
    public ILocator PageInfo => _page.Locator(".page-info");
    public ILocator NoApplicationsMessage => _page.Locator(".alert:has-text('No applications')");

    public ILocator ReviewTabs => _page.Locator("[data-testid=review-tabs]");
    public ILocator InitialQueueTab => _page.Locator("[data-testid=review-tab-initial]");
    public ILocator SigningInboxTab => _page.Locator("[data-testid=review-tab-signing]");

    public async Task GotoAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Review");
    }

    public ILocator ReviewLink(int applicationId)
    {
        return _page.Locator($"a[href*='Review/{applicationId}']");
    }

    public async Task<int> GetQueueCountAsync()
    {
        return await QueueRows.CountAsync();
    }

    public async Task ClickSigningInboxTab()
    {
        await SigningInboxTab.ClickAsync();
    }

    public async Task<bool> IsSigningInboxTabActive()
    {
        var classAttr = await SigningInboxTab.GetAttributeAsync("class");
        return classAttr is not null && classAttr.Contains("active");
    }

    public async Task<bool> IsInitialQueueTabActive()
    {
        var classAttr = await InitialQueueTab.GetAttributeAsync("class");
        return classAttr is not null && classAttr.Contains("active");
    }

    public ILocator GenerateAgreementTab => _page.Locator("[data-testid=review-tab-generate]");
    public ILocator GenerateAgreementTable => _page.Locator("[data-testid=generate-agreement-table]");
    public ILocator GenerateAgreementRows => _page.Locator("[data-testid=generate-agreement-row]");
    public ILocator GenerateAgreementEmpty => _page.Locator("[data-testid=generate-agreement-empty]");

    public async Task ClickGenerateAgreementTab()
    {
        await GenerateAgreementTab.ClickAsync();
    }

    public async Task<bool> IsGenerateAgreementTabActive()
    {
        var classAttr = await GenerateAgreementTab.GetAttributeAsync("class");
        return classAttr is not null && classAttr.Contains("active");
    }

    public async Task GotoGenerateAgreementAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Review/GenerateAgreement");
    }
}
