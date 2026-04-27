using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects.Admin;

public class AdminUserCreatePage : AdminBasePage
{
    public AdminUserCreatePage(IPage page) : base(page)
    {
    }

    public ILocator FirstName => Page.Locator("input[name=\"FirstName\"]");
    public ILocator LastName => Page.Locator("input[name=\"LastName\"]");
    public ILocator Email => Page.Locator("input[name=\"Email\"]");
    public ILocator Phone => Page.Locator("input[name=\"Phone\"]");
    public ILocator Role => Page.Locator("select[name=\"Role\"]");
    public ILocator InitialPassword => Page.Locator("input[name=\"InitialPassword\"]");
    public ILocator LegalId => Page.Locator("input[name=\"LegalId\"]");
    public ILocator LegalIdField => Page.Locator("[data-testid=\"legalid-field\"]");
    public ILocator SubmitButton => Page.Locator("[data-testid=\"admin-user-create-submit\"]");
    public ILocator ValidationSummary => Page.Locator(".validation-summary-errors, .text-danger");

    public Task GoToAsync(string baseUrl) =>
        Page.GotoAsync($"{baseUrl}/Admin/Users/Create");

    public async Task FillAsync(
        string firstName,
        string lastName,
        string email,
        string? phone,
        string role,
        string initialPassword,
        string? legalId)
    {
        await FirstName.FillAsync(firstName);
        await LastName.FillAsync(lastName);
        await Email.FillAsync(email);
        if (phone is not null)
        {
            await Phone.FillAsync(phone);
        }
        await Role.SelectOptionAsync(role);
        await InitialPassword.FillAsync(initialPassword);
        if (legalId is not null)
        {
            await LegalId.FillAsync(legalId);
        }
    }

    public Task SubmitAsync() => SubmitButton.ClickAsync();
}
