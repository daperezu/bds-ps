using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects.Admin.Reports;

public class AdminReportsDashboardPage : AdminBasePage
{
    public AdminReportsDashboardPage(IPage page) : base(page) { }

    public ILocator PipelineRow    => Page.Locator("[data-testid=dashboard-row-pipeline]");
    public ILocator FinancialRow   => Page.Locator("[data-testid=dashboard-row-financial]");
    public ILocator ApplicantsRow  => Page.Locator("[data-testid=dashboard-row-applicants]");
    public ILocator SubTabs        => Page.Locator("[data-testid=report-subtabs]");
    public ILocator FromInput      => Page.Locator("[data-testid=dashboard-range-from]");
    public ILocator ToInput        => Page.Locator("[data-testid=dashboard-range-to]");
    public ILocator ApplyButton    => Page.Locator("[data-testid=dashboard-range-apply]");

    public Task GoToAsync(string baseUrl) => Page.GotoAsync($"{baseUrl}/Admin/Reports");

    public Task GoToWithRangeAsync(string baseUrl, string from, string to)
        => Page.GotoAsync($"{baseUrl}/Admin/Reports?from={from}&to={to}");

    public ILocator KpiTile(string label)
        => Page.Locator($"[data-testid=kpi-tile][data-kpi-label='{label}']");

    public async Task<int?> ReadKpiNumericAsync(string label)
    {
        var tile = KpiTile(label);
        var numericNode = tile.Locator("[data-testid=kpi-tile-numeric]");
        if (await numericNode.CountAsync() == 0) return null;
        var text = (await numericNode.TextContentAsync())?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return int.Parse(text.Replace(",", string.Empty).Replace(".", string.Empty));
    }

    public async Task<IReadOnlyList<(string Currency, string Amount)>> ReadKpiStackAsync(string label)
    {
        var tile = KpiTile(label);
        var lines = tile.Locator("[data-testid=kpi-tile-stack] li");
        var count = await lines.CountAsync();
        var rows = new List<(string, string)>();
        for (var i = 0; i < count; i++)
        {
            var li = lines.Nth(i);
            var currency = await li.GetAttributeAsync("data-currency") ?? string.Empty;
            var amount = (await li.Locator("span").Last.TextContentAsync())?.Trim() ?? string.Empty;
            rows.Add((currency, amount));
        }
        return rows;
    }

    public async Task SetDateRangeAsync(string from, string to)
    {
        await FromInput.FillAsync(from);
        await ToInput.FillAsync(to);
        await ApplyButton.ClickAsync();
    }

    public ILocator SubTab(string key) =>
        SubTabs.Locator($"[data-testid=report-subtab][data-tab='{key}']");
}
