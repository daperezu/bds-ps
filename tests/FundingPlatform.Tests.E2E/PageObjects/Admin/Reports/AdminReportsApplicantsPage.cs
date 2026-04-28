using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects.Admin.Reports;

public class AdminReportsApplicantsPage : AdminBasePage
{
    public AdminReportsApplicantsPage(IPage page) : base(page) { }

    public ILocator FilterForm  => Page.Locator("[data-testid=applicants-filter]");
    public ILocator SearchInput => Page.Locator("[data-testid=applicants-filter-search]");
    public ILocator ApplyButton => Page.Locator("[data-testid=applicants-filter-apply]");
    public ILocator Table       => Page.Locator("[data-testid=applicants-table]");
    public ILocator Rows        => Page.Locator("[data-testid=applicants-row]");
    public ILocator ExportLink  => Page.Locator("[data-testid=applicants-export-csv]");

    public Task GoToAsync(string baseUrl) => Page.GotoAsync($"{baseUrl}/Admin/Reports/Applicants");
}
