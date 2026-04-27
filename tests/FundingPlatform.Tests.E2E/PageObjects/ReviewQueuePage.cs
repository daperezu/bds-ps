using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ReviewQueuePage : BasePage
{
    public ReviewQueuePage(IPage page) : base(page)
    {
    }

    public ILocator QueueTable => Page.Locator("table.review-queue");
    public ILocator QueueRows => Page.Locator("table.review-queue tbody tr");
    public ILocator PaginationLinks => Page.Locator("nav.pagination a");
    public ILocator PageInfo => Page.Locator(".page-info");
    public ILocator NoApplicationsMessage => Page.Locator(".alert:has-text('No applications')");

    public ILocator ReviewTabs => Page.Locator("[data-testid=review-tabs]");
    public ILocator InitialQueueTab => Page.Locator("[data-testid=review-tab-initial]");
    public ILocator SigningInboxTab => Page.Locator("[data-testid=review-tab-signing]");

    public async Task GotoAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Review");
    }

    public ILocator ReviewLink(int applicationId)
    {
        return Page.Locator($"a[href*='Review/{applicationId}']");
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

    public ILocator GenerateAgreementTab => Page.Locator("[data-testid=review-tab-generate]");
    public ILocator GenerateAgreementTable => Page.Locator("[data-testid=generate-agreement-table]");
    public ILocator GenerateAgreementRows => Page.Locator("[data-testid=generate-agreement-row]");
    public ILocator GenerateAgreementEmpty => Page.Locator("[data-testid=generate-agreement-empty]");

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
        await Page.GotoAsync($"{baseUrl}/Review/GenerateAgreement");
    }
}
