using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class RegisterPage
{
    private readonly IPage _page;

    public RegisterPage(IPage page)
    {
        _page = page;
    }

    public ILocator EmailInput => _page.Locator("[name=Email]");
    public ILocator PasswordInput => _page.Locator("[name=Password]");
    public ILocator ConfirmPasswordInput => _page.Locator("[name=ConfirmPassword]");
    public ILocator FirstNameInput => _page.Locator("[name=FirstName]");
    public ILocator LastNameInput => _page.Locator("[name=LastName]");
    public ILocator LegalIdInput => _page.Locator("[name=LegalId]");
    public ILocator SubmitButton => _page.Locator("main button[type=submit]");

    public async Task GotoAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Account/Register");
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
