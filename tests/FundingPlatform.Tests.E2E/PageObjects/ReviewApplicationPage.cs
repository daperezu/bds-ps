using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ReviewApplicationPage : BasePage
{
    public ReviewApplicationPage(IPage page) : base(page)
    {
    }

    public ILocator ApplicantName => Page.Locator(".applicant-name");
    public ILocator PerformanceScore => Page.Locator(".performance-score");
    public ILocator ApplicationState => Page.Locator(".application-state .badge");
    public ILocator ItemCards => Page.Locator(".review-item");
    public ILocator SendBackButton => Page.Locator("button:has-text('Send Back')");
    public ILocator FinalizeButton => Page.Locator("button:has-text('Finalize Review')");
    public ILocator ForceFinalizationConfirm => Page.Locator("#forceFinalizationConfirm");
    public ILocator UnresolvedWarning => Page.Locator(".unresolved-warning");
    public ILocator SuccessMessage => Page.Locator(".alert-success");
    public ILocator ErrorMessage => Page.Locator(".alert-danger");

    public async Task GotoAsync(string baseUrl, int applicationId)
    {
        await Page.GotoAsync($"{baseUrl}/Review/{applicationId}");
    }

    private ILocator ItemCard(int itemId) =>
        Page.Locator($".review-item[data-item-id='{itemId}']");

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
        return Page.Locator($"button[data-item-id='{itemId}'].submit-decision");
    }

    public ILocator ItemReviewStatusBadge(int itemId)
    {
        return ItemCard(itemId).Locator(".review-status-badge");
    }

    public ILocator TechnicalEquivalenceSubmit(int itemId)
    {
        return Page.Locator($"button[data-item-id='{itemId}'].submit-equivalence");
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
