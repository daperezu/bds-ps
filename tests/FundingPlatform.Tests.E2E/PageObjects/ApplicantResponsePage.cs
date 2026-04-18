using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ApplicantResponsePage
{
    private readonly IPage _page;

    public ApplicantResponsePage(IPage page)
    {
        _page = page;
    }

    public ILocator Header => _page.Locator(".applicant-response-header");
    public ILocator ApplicationState => _page.Locator(".application-state");
    public ILocator SubmitButton => _page.Locator(".submit-response");
    public ILocator OpenAppealButton => _page.Locator(".open-appeal-button");
    public ILocator AppealFrozenBanner => _page.Locator(".appeal-frozen-banner");
    public ILocator SubmittedAt => _page.Locator(".submitted-at");
    public ILocator ItemRows => _page.Locator("tr.response-item");
    public ILocator SuccessMessage => _page.Locator(".alert-success");
    public ILocator ErrorMessage => _page.Locator(".alert-danger");

    public async Task GotoAsync(string baseUrl, int applicationId)
    {
        await _page.GotoAsync($"{baseUrl}/ApplicantResponse/Index/{applicationId}");
    }

    public ILocator ItemRow(int itemId) =>
        _page.Locator($"tr.response-item[data-item-id='{itemId}']");

    public ILocator AcceptRadio(int itemId) =>
        ItemRow(itemId).Locator("input.decision-accept");

    public ILocator RejectRadio(int itemId) =>
        ItemRow(itemId).Locator("input.decision-reject");

    public ILocator DecisionDisplay(int itemId) =>
        ItemRow(itemId).Locator(".decision-display");

    public async Task SubmitAsync()
    {
        await SubmitButton.ClickAsync();
    }
}
