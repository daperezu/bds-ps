using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class LoginPage : BasePage
{
    public LoginPage(IPage page) : base(page)
    {
    }

    public ILocator AuthShell => Page.Locator("[data-testid=\"auth-shell\"]");

    public async Task<bool> IsAuthShellVisibleAsync()
    {
        if (await Sidebar.CountAsync() > 0) return false;
        return await AuthShell.CountAsync() > 0;
    }

    public ILocator EmailInput => Page.Locator("[name=Email]");
    public ILocator PasswordInput => Page.Locator("[name=Password]");
    public ILocator SubmitButton => Page.Locator("main button[type=submit]");

    public async Task GotoAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Account/Login");
    }

    public async Task LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await SubmitButton.ClickAsync();
    }
}
