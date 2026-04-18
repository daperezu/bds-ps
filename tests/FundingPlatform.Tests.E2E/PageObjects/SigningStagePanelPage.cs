using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class SigningStagePanelPage
{
    private readonly IPage _page;

    public SigningStagePanelPage(IPage page)
    {
        _page = page;
    }

    public ILocator Panel => _page.Locator("#funding-agreement-panel");
    public ILocator ExecutedBadge => _page.Locator("[data-testid=funding-agreement-executed-badge]");
    public ILocator PendingCard => _page.Locator("[data-testid=signed-upload-pending]");
    public ILocator UploadInput => _page.Locator("[data-testid=signed-upload-file]");
    public ILocator UploadSubmitButton => _page.Locator("[data-testid=signed-upload-submit]");
    public ILocator ReplaceInput => _page.Locator("[data-testid=signed-upload-replace-file]");
    public ILocator ReplaceSubmitButton => _page.Locator("[data-testid=signed-upload-replace]");
    public ILocator WithdrawButton => _page.Locator("[data-testid=signed-upload-withdraw]");
    public ILocator ApproveButton => _page.Locator("[data-testid=signed-upload-approve]");
    public ILocator ApproveCommentInput => _page.Locator("[data-testid=signed-upload-approve-comment]");
    public ILocator RejectButton => _page.Locator("[data-testid=signed-upload-reject]");
    public ILocator RejectCommentInput => _page.Locator("[data-testid=signed-upload-reject-comment]");
    public ILocator LastRejectionNotice => _page.Locator("[data-testid=signed-upload-last-rejection]");
    public ILocator SignedDownloadLink => _page.Locator("[data-testid=signed-agreement-download]");
    public ILocator VersionMismatchHint => _page.Locator("[data-testid=signed-upload-version-mismatch]");

    public async Task UploadSigned(string filePath)
    {
        _page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await UploadInput.SetInputFilesAsync(filePath);
        await UploadSubmitButton.ClickAsync();
    }

    public async Task<bool> IsPendingUploadVisible()
    {
        return await PendingCard.CountAsync() > 0;
    }

    public async Task ApprovePending(string? comment = null)
    {
        if (!string.IsNullOrWhiteSpace(comment))
        {
            await ApproveCommentInput.FillAsync(comment);
        }
        await ApproveButton.ClickAsync();
    }

    public async Task RejectPending(string comment)
    {
        await RejectCommentInput.FillAsync(comment);
        await RejectButton.ClickAsync();
    }

    public async Task ReplacePending(string filePath)
    {
        _page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await ReplaceInput.SetInputFilesAsync(filePath);
        await ReplaceSubmitButton.ClickAsync();
    }

    public async Task WithdrawPending()
    {
        _page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await WithdrawButton.ClickAsync();
    }

    public async Task<bool> IsExecutedBadgeVisible()
    {
        return await ExecutedBadge.CountAsync() > 0;
    }

    public async Task<bool> IsRejectionCommentVisible(string expectedComment)
    {
        if (await LastRejectionNotice.CountAsync() == 0) return false;
        var text = await LastRejectionNotice.TextContentAsync();
        return text is not null && text.Contains(expectedComment, StringComparison.OrdinalIgnoreCase);
    }

    public ILocator SignedDownload => SignedDownloadLink;
}
