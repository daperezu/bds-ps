using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ReviewApplicationPage
{
    private readonly IPage _page;

    public ReviewApplicationPage(IPage page)
    {
        _page = page;
    }

    public ILocator ApplicantName => _page.Locator(".applicant-name");
    public ILocator PerformanceScore => _page.Locator(".performance-score");
    public ILocator ApplicationState => _page.Locator(".application-state .badge");
    public ILocator ItemCards => _page.Locator(".review-item");
    public ILocator SendBackButton => _page.Locator("button:has-text('Send Back')");
    public ILocator FinalizeButton => _page.Locator("button:has-text('Finalize Review')");
    public ILocator ForceFinalizationConfirm => _page.Locator("#forceFinalizationConfirm");
    public ILocator UnresolvedWarning => _page.Locator(".unresolved-warning");
    public ILocator SuccessMessage => _page.Locator(".alert-success");
    public ILocator ErrorMessage => _page.Locator(".alert-danger");

    public async Task GotoAsync(string baseUrl, int applicationId)
    {
        await _page.GotoAsync($"{baseUrl}/Review/{applicationId}");
    }

    private ILocator ItemCard(int itemId) =>
        _page.Locator($".review-item[data-item-id='{itemId}']");

    public ILocator ItemDecisionRadio(int itemId, string decision)
    {
        return ItemCard(itemId).Locator($"input[name='Decision'][value='{decision}']");
    }

    public ILocator ItemSupplierDropdown(int itemId)
    {
        return ItemCard(itemId).Locator("select[name='SelectedSupplierId']");
    }

    public ILocator ItemCommentField(int itemId)
    {
        return ItemCard(itemId).Locator("textarea[name='Comment']");
    }

    public ILocator ItemSubmitButton(int itemId)
    {
        return _page.Locator($"button[data-item-id='{itemId}'].submit-decision");
    }

    public ILocator ItemReviewStatusBadge(int itemId)
    {
        return ItemCard(itemId).Locator(".review-status-badge");
    }

    public ILocator TechnicalEquivalenceSubmit(int itemId)
    {
        return _page.Locator($"button[data-item-id='{itemId}'].submit-equivalence");
    }

    public ILocator RecommendedBadge(int itemId)
    {
        return ItemCard(itemId).Locator(".recommended-badge");
    }

    public ILocator QuotationRows(int itemId)
    {
        return ItemCard(itemId).Locator(".quotation-row");
    }

    public ILocator SupplierScores(int itemId)
    {
        return ItemCard(itemId).Locator(".supplier-score");
    }

    public ILocator ScoreBreakdowns(int itemId)
    {
        return ItemCard(itemId).Locator(".score-breakdown");
    }
}
