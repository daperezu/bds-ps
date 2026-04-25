using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class AppealThreadPage : BasePage
{
    public AppealThreadPage(IPage page) : base(page)
    {
    }

    public ILocator Header => Page.Locator(".appeal-header");
    public ILocator AppealStatus => Page.Locator(".appeal-status");
    public ILocator AppealResolution => Page.Locator(".appeal-resolution");
    public ILocator Messages => Page.Locator(".appeal-message");
    public ILocator MessageTextArea => Page.Locator("#NewMessageText");
    public ILocator PostMessageButton => Page.Locator(".post-message-button");
    public ILocator UpholdButton => Page.Locator(".resolve-uphold");
    public ILocator GrantReopenDraftButton => Page.Locator(".resolve-reopen-draft");
    public ILocator GrantReopenReviewButton => Page.Locator(".resolve-reopen-review");
    public ILocator SuccessMessage => Page.Locator(".alert-success");
    public ILocator ErrorMessage => Page.Locator(".alert-danger");

    public async Task GotoAsync(string baseUrl, int applicationId)
    {
        await Page.GotoAsync($"{baseUrl}/ApplicantResponse/Appeal/{applicationId}");
    }

    public async Task PostMessageAsync(string text)
    {
        await MessageTextArea.FillAsync(text);
        await PostMessageButton.ClickAsync();
    }

    public ILocator ApplicantMessages => Page.Locator(".appeal-message.by-applicant");
    public ILocator ReviewerMessages => Page.Locator(".appeal-message.by-reviewer");
}
