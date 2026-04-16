using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class SupplierPage
{
    private readonly IPage _page;

    public SupplierPage(IPage page)
    {
        _page = page;
    }

    public ILocator SupplierLegalIdInput => _page.Locator("[name=SupplierLegalId]");
    public ILocator SupplierNameInput => _page.Locator("[name=SupplierName]");
    public ILocator ContactNameInput => _page.Locator("[name=ContactName]");
    public ILocator EmailInput => _page.Locator("[name=Email]");
    public ILocator PhoneInput => _page.Locator("[name=Phone]");
    public ILocator LocationInput => _page.Locator("[name=Location]");
    public ILocator HasElectronicInvoiceCheckbox => _page.Locator("input[type=checkbox][name=HasElectronicInvoice]");
    public ILocator ShippingDetailsInput => _page.Locator("[name=ShippingDetails]");
    public ILocator WarrantyInfoInput => _page.Locator("[name=WarrantyInfo]");
    public ILocator IsCompliantCCSSCheckbox => _page.Locator("input[type=checkbox][name=IsCompliantCCSS]");
    public ILocator IsCompliantHaciendaCheckbox => _page.Locator("input[type=checkbox][name=IsCompliantHacienda]");
    public ILocator IsCompliantSICOPCheckbox => _page.Locator("input[type=checkbox][name=IsCompliantSICOP]");
    public ILocator PriceInput => _page.Locator("[name=Price]");
    public ILocator ValidUntilInput => _page.Locator("[name=ValidUntil]");
    public ILocator QuotationFileInput => _page.Locator("[name=QuotationFile]");
    public ILocator SubmitButton => _page.Locator("main button[type=submit]");
    public ILocator ValidationSummary => _page.Locator(".text-danger");

    public async Task NavigateToAddAsync(int appId, int itemId, string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Application/{appId}/Item/{itemId}/Supplier/Add");
    }

    public async Task FillSupplierFormAsync(
        string legalId,
        string name,
        decimal price,
        string validUntil,
        string filePath,
        string? contactName = null,
        string? email = null,
        string? phone = null,
        string? location = null,
        bool isCompliantCCSS = false,
        bool isCompliantHacienda = false,
        bool isCompliantSICOP = false)
    {
        await SupplierLegalIdInput.FillAsync(legalId);
        await SupplierNameInput.FillAsync(name);

        if (contactName is not null) await ContactNameInput.FillAsync(contactName);
        if (email is not null) await EmailInput.FillAsync(email);
        if (phone is not null) await PhoneInput.FillAsync(phone);
        if (location is not null) await LocationInput.FillAsync(location);

        if (isCompliantCCSS) await IsCompliantCCSSCheckbox.CheckAsync();
        if (isCompliantHacienda) await IsCompliantHaciendaCheckbox.CheckAsync();
        if (isCompliantSICOP) await IsCompliantSICOPCheckbox.CheckAsync();

        await PriceInput.FillAsync(price.ToString());
        await ValidUntilInput.FillAsync(validUntil);
        await QuotationFileInput.SetInputFilesAsync(filePath);
    }

    public async Task SubmitAsync()
    {
        await SubmitButton.ClickAsync();
    }
}
