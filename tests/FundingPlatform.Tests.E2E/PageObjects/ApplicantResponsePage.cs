using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ApplicantResponsePage : BasePage
{
    public ApplicantResponsePage(IPage page) : base(page)
    {
    }

    public ILocator Header => Page.Locator(".applicant-response-header");
    public ILocator ApplicationState => Page.Locator(".application-state");
    public ILocator SubmitButton => Page.Locator(".submit-response");
    public ILocator OpenAppealButton => Page.Locator(".open-appeal-button");
    public ILocator AppealFrozenBanner => Page.Locator(".appeal-frozen-banner");
    public ILocator SubmittedAt => Page.Locator(".submitted-at");
    public ILocator ItemRows => Page.Locator("tr.response-item");
    public ILocator SuccessMessage => Page.Locator(".alert-success");
    public ILocator ErrorMessage => Page.Locator(".alert-danger");

    public ILocator ReadyToSignBanner => Page.Locator("[data-testid=signing-banner-ready]");
    public ILocator AgreementExecutedBanner => Page.Locator("[data-testid=signing-banner-executed]");

    public SigningStagePanelPage SigningPanel => new(Page);

    public async Task GotoAsync(string baseUrl, int applicationId)
    {
        await Page.GotoAsync($"{baseUrl}/ApplicantResponse/Index/{applicationId}");
    }

    public async Task<bool> IsReadyToSignBannerVisible()
    {
        return await ReadyToSignBanner.CountAsync() > 0;
    }

    public async Task<bool> IsAgreementExecutedBannerVisible()
    {
        return await AgreementExecutedBanner.CountAsync() > 0;
    }

    public ILocator ItemRow(int itemId) =>
        Page.Locator($"tr.response-item[data-item-id='{itemId}']");

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
