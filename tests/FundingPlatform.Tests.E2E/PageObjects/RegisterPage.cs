using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class RegisterPage : BasePage
{
    public RegisterPage(IPage page) : base(page)
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
    public ILocator ConfirmPasswordInput => Page.Locator("[name=ConfirmPassword]");
    public ILocator FirstNameInput => Page.Locator("[name=FirstName]");
    public ILocator LastNameInput => Page.Locator("[name=LastName]");
    public ILocator LegalIdInput => Page.Locator("[name=LegalId]");
    public ILocator SubmitButton => Page.Locator("main button[type=submit]");

    public async Task GotoAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Account/Register");
    }

    public async Task RegisterAsync(string email, string password, string firstName, string lastName, string legalId)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await ConfirmPasswordInput.FillAsync(password);
        await FirstNameInput.FillAsync(firstName);
        await LastNameInput.FillAsync(lastName);
        await LegalIdInput.FillAsync(legalId);
        await SubmitButton.ClickAsync();
    }
}
