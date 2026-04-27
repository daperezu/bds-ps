using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class QuotationPage : BasePage
{
    public QuotationPage(IPage page) : base(page)
    {
    }

    public ILocator PriceInput => Page.Locator("[name=Price]");
    public ILocator ValidUntilInput => Page.Locator("[name=ValidUntil]");
    public ILocator QuotationFileInput => Page.Locator("[name=QuotationFile]");
    public ILocator SubmitButton => Page.Locator("main button[type=submit]");
    public ILocator ValidationSummary => Page.Locator(".text-danger");

    public async Task NavigateToAddAsync(int appId, int itemId, int supplierId, string supplierName, string baseUrl)
    {
        var encodedName = Uri.EscapeDataString(supplierName);
        await Page.GotoAsync($"{baseUrl}/Application/{appId}/Item/{itemId}/Quotation/Add?supplierId={supplierId}&supplierName={encodedName}");
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
