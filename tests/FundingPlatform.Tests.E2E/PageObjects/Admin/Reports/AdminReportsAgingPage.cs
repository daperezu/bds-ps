using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects.Admin.Reports;

public class AdminReportsAgingPage : AdminBasePage
{
    public AdminReportsAgingPage(IPage page) : base(page) { }

    public ILocator FilterForm     => Page.Locator("[data-testid=aging-filter]");
    public ILocator ThresholdInput => Page.Locator("[data-testid=aging-filter-threshold]");
    public ILocator ApplyButton    => Page.Locator("[data-testid=aging-filter-apply]");
    public ILocator Table          => Page.Locator("[data-testid=aging-table]");
    public ILocator Rows           => Page.Locator("[data-testid=aging-row]");
    public ILocator ExportLink     => Page.Locator("[data-testid=aging-export-csv]");
    public ILocator FilterError    => Page.Locator("[data-testid=aging-filter-error]");

    public Task GoToAsync(string baseUrl) => Page.GotoAsync($"{baseUrl}/Admin/Reports/Aging");

    public Task GoToWithThresholdAsync(string baseUrl, int threshold) =>
        Page.GotoAsync($"{baseUrl}/Admin/Reports/Aging?threshold={threshold}");
}
