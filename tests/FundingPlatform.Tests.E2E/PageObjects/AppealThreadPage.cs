using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class AppealThreadPage
{
    private readonly IPage _page;

    public AppealThreadPage(IPage page)
    {
        _page = page;
    }

    public ILocator Header => _page.Locator(".appeal-header");
    public ILocator AppealStatus => _page.Locator(".appeal-status");
    public ILocator AppealResolution => _page.Locator(".appeal-resolution");
    public ILocator Messages => _page.Locator(".appeal-message");
    public ILocator MessageTextArea => _page.Locator("#NewMessageText");
    public ILocator PostMessageButton => _page.Locator(".post-message-button");
    public ILocator UpholdButton => _page.Locator(".resolve-uphold");
    public ILocator GrantReopenDraftButton => _page.Locator(".resolve-reopen-draft");
    public ILocator GrantReopenReviewButton => _page.Locator(".resolve-reopen-review");
    public ILocator SuccessMessage => _page.Locator(".alert-success");
    public ILocator ErrorMessage => _page.Locator(".alert-danger");

    public async Task GotoAsync(string baseUrl, int applicationId)
    {
        await _page.GotoAsync($"{baseUrl}/ApplicantResponse/Appeal/{applicationId}");
    }

    public async Task PostMessageAsync(string text)
    {
        await MessageTextArea.FillAsync(text);
        await PostMessageButton.ClickAsync();
    }

    public ILocator ApplicantMessages => _page.Locator(".appeal-message.by-applicant");
    public ILocator ReviewerMessages => _page.Locator(".appeal-message.by-reviewer");
}
