using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects.Admin;

public class AdminReportsPage : AdminBasePage
{
    public AdminReportsPage(IPage page) : base(page)
    {
    }

    public Task GoToAsync(string baseUrl) =>
        Page.GotoAsync($"{baseUrl}/Admin/Reports");
}
