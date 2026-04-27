using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class FundingAgreementPanelPage : BasePage
{
    public FundingAgreementPanelPage(IPage page) : base(page)
    {
    }

    public ILocator Panel => Page.Locator("#funding-agreement-panel");
    public ILocator GenerateButton => Page.Locator("[data-testid=funding-agreement-generate]");
    public ILocator RegenerateButton => Page.Locator("[data-testid=funding-agreement-regenerate]");
    public ILocator DownloadLink => Page.Locator("[data-testid=funding-agreement-download]");
    public ILocator DisabledReason => Page.Locator("[data-testid=funding-agreement-disabled-reason]");
    public ILocator Metadata => Page.Locator("[data-testid=funding-agreement-metadata]");
    public ILocator ErrorBanner => Page.Locator("[data-testid=funding-agreement-error]");
    public ILocator SuccessBanner => Page.Locator("[data-testid=funding-agreement-success]");

    public async Task<bool> IsPanelVisibleAsync()
    {
        try
        {
            await Panel.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public async Task<string?> GetDisabledReasonAsync()
    {
        if (await DisabledReason.CountAsync() == 0) return null;
        return await DisabledReason.TextContentAsync();
    }

    public async Task ClickGenerateAsync()
    {
        await GenerateButton.ClickAsync();
    }

    public async Task<bool> HasDownloadLinkAsync()
    {
        return await DownloadLink.CountAsync() > 0;
    }

    public async Task<string?> GetGeneratedAtMetadataAsync()
    {
        if (await Metadata.CountAsync() == 0) return null;
        return await Metadata.TextContentAsync();
    }

    public async Task GotoDetailsAsync(string baseUrl, int applicationId)
    {
        await Page.GotoAsync($"{baseUrl}/Applications/{applicationId}/FundingAgreement");
    }
}
