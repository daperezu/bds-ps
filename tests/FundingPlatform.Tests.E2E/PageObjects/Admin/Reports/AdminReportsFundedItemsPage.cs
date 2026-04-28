using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects.Admin.Reports;

public class AdminReportsFundedItemsPage : AdminBasePage
{
    public AdminReportsFundedItemsPage(IPage page) : base(page) { }

    public ILocator FilterForm   => Page.Locator("[data-testid=funded-items-filter]");
    public ILocator ExecutedOnly => Page.Locator("[data-testid=funded-items-filter-executedonly]");
    public ILocator ApplyButton  => Page.Locator("[data-testid=funded-items-filter-apply]");
    public ILocator Table        => Page.Locator("[data-testid=funded-items-table]");
    public ILocator Rows         => Page.Locator("[data-testid=funded-items-row]");
    public ILocator ExportLink   => Page.Locator("[data-testid=funded-items-export-csv]");

    public Task GoToAsync(string baseUrl) => Page.GotoAsync($"{baseUrl}/Admin/Reports/FundedItems");
}
