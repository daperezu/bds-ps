using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    public ILocator EmailInput => _page.Locator("[name=Email]");
    public ILocator PasswordInput => _page.Locator("[name=Password]");
    public ILocator SubmitButton => _page.Locator("main button[type=submit]");

    public async Task GotoAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Account/Login");
    }

    public async Task LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await SubmitButton.ClickAsync();
    }
}
