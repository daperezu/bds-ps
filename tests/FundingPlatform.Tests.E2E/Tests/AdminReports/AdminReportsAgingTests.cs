using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.Helpers;
using FundingPlatform.Tests.E2E.PageObjects.Admin.Reports;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests.AdminReports;

[Category("AdminReports")]
public class AdminReportsAgingTests : AuthenticatedTestBase
{
    private const string AdminEmail = "admin@FundingPlatform.com";
    private const string AdminPassword = "Sentinel123!";

    [Test]
    public async Task Aging_RendersFilterAndExport()
    {
        await LoginAsync(Page, AdminEmail, AdminPassword);
        var page = new AdminReportsAgingPage(Page);
        await page.GoToAsync(BaseUrl);

        await Expect(page.FilterForm).ToBeVisibleAsync();
        Assert.That(await page.ThresholdInput.InputValueAsync(), Is.EqualTo("14"),
            "Threshold default must be 14 days.");
    }

    [Test]
    public async Task Aging_ThresholdOutOfRange_RendersWithSafeDefaults()
    {
        await LoginAsync(Page, AdminEmail, AdminPassword);
        var page = new AdminReportsAgingPage(Page);
        await page.GoToWithThresholdAsync(BaseUrl, threshold: 999);

        // Controller catches the invalid threshold and surfaces the error banner with safe defaults.
        await Expect(page.FilterError).ToBeVisibleAsync();
    }

    [Test]
    public async Task Aging_CsvExport_HasExpectedHeader()
    {
        await LoginAsync(Page, AdminEmail, AdminPassword);
        var page = new AdminReportsAgingPage(Page);
        await page.GoToAsync(BaseUrl);

        var downloadTask = Page.WaitForDownloadAsync();
        await page.ExportLink.ClickAsync();
        var download = await downloadTask;
        var path = await download.PathAsync();
        Assert.That(path, Is.Not.Null.And.Not.Empty);
        var bytes = await File.ReadAllBytesAsync(path!);

        CsvAssertions.AssertHeaderEquals(bytes,
            "App Id", "Applicant Name", "Email", "Legal Id", "State",
            "Days In Current State", "Last Transition", "Last Actor", "Item Count",
            "Approved Amount", "Currency");
    }
}
