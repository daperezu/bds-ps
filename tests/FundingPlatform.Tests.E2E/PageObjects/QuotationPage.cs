using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class QuotationPage
{
    private readonly IPage _page;

    public QuotationPage(IPage page)
    {
        _page = page;
    }

    public ILocator PriceInput => _page.Locator("[name=Price]");
    public ILocator ValidUntilInput => _page.Locator("[name=ValidUntil]");
    public ILocator QuotationFileInput => _page.Locator("[name=QuotationFile]");
    public ILocator SubmitButton => _page.Locator("main button[type=submit]");
    public ILocator ValidationSummary => _page.Locator(".text-danger");

    public async Task NavigateToAddAsync(int appId, int itemId, int supplierId, string supplierName, string baseUrl)
    {
        var encodedName = Uri.EscapeDataString(supplierName);
        await _page.GotoAsync($"{baseUrl}/Application/{appId}/Item/{itemId}/Quotation/Add?supplierId={supplierId}&supplierName={encodedName}");
    }

    public async Task FillQuotationFormAsync(decimal price, string validUntil, string filePath)
    {
        await PriceInput.FillAsync(price.ToString());
        await ValidUntilInput.FillAsync(validUntil);
        await QuotationFileInput.SetInputFilesAsync(filePath);
    }

    public async Task SubmitAsync()
    {
        await SubmitButton.ClickAsync();
    }
}
