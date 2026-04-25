using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public abstract class BasePage
{
    protected readonly IPage Page;

    protected BasePage(IPage page)
    {
        Page = page;
    }

    public ILocator Sidebar => Page.Locator("[data-testid=\"sidebar\"]");
    public ILocator Topbar => Page.Locator("[data-testid=\"topbar\"]");
    public ILocator PageTitle => Page.Locator("[data-testid=\"page-title\"]");
    public ILocator BreadcrumbContainer => Page.Locator("[data-testid=\"breadcrumbs\"]");

    public ILocator SidebarEntry(string slug) => Page.Locator($"[data-testid=\"sidebar-entry-{slug}\"]");
}
