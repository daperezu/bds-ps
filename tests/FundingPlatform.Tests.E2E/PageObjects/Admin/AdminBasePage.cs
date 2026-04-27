using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects.Admin;

public abstract class AdminBasePage : BasePage
{
    protected AdminBasePage(IPage page) : base(page)
    {
    }

    public ILocator AdminAreaWrapper => Page.Locator("[data-testid=\"admin-area\"]");
    public ILocator PrimaryAction => Page.Locator("[data-testid=\"page-primary-action\"]");
    public ILocator EmptyState => Page.Locator("[data-testid=\"empty-state\"]");
    public ILocator ConfirmDialog(string id) => Page.Locator($"#{id}");
}
