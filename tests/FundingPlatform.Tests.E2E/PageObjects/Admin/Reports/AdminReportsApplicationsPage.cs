using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects.Admin.Reports;

public class AdminReportsApplicationsPage : AdminBasePage
{
    public AdminReportsApplicationsPage(IPage page) : base(page) { }

    public ILocator FilterForm   => Page.Locator("[data-testid=applications-filter]");
    public ILocator SearchInput  => Page.Locator("[data-testid=applications-filter-search]");
    public ILocator StatesSelect => Page.Locator("[data-testid=applications-filter-states]");
    public ILocator ApplyButton  => Page.Locator("[data-testid=applications-filter-apply]");
    public ILocator Table        => Page.Locator("[data-testid=applications-table]");
    public ILocator Rows         => Page.Locator("[data-testid=applications-row]");
    public ILocator ExportLink   => Page.Locator("[data-testid=applications-export-csv]");
    public ILocator Pagination   => Page.Locator("[data-testid=applications-pagination]");

    public Task GoToAsync(string baseUrl) => Page.GotoAsync($"{baseUrl}/Admin/Reports/Applications");

    public Task GoToWithQueryAsync(string baseUrl, string query) =>
        Page.GotoAsync($"{baseUrl}/Admin/Reports/Applications?{query}");
}
