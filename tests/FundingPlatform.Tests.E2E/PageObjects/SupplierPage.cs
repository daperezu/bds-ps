using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class SupplierPage : BasePage
{
    public SupplierPage(IPage page) : base(page)
    {
    }

    public ILocator SupplierLegalIdInput => Page.Locator("[name=SupplierLegalId]");
    public ILocator SupplierNameInput => Page.Locator("[name=SupplierName]");
    public ILocator ContactNameInput => Page.Locator("[name=ContactName]");
    public ILocator EmailInput => Page.Locator("[name=Email]");
    public ILocator PhoneInput => Page.Locator("[name=Phone]");
    public ILocator LocationInput => Page.Locator("[name=Location]");
    public ILocator HasElectronicInvoiceCheckbox => Page.Locator("input[type=checkbox][name=HasElectronicInvoice]");
    public ILocator ShippingDetailsInput => Page.Locator("[name=ShippingDetails]");
    public ILocator WarrantyInfoInput => Page.Locator("[name=WarrantyInfo]");
    public ILocator IsCompliantCCSSCheckbox => Page.Locator("input[type=checkbox][name=IsCompliantCCSS]");
    public ILocator IsCompliantHaciendaCheckbox => Page.Locator("input[type=checkbox][name=IsCompliantHacienda]");
    public ILocator IsCompliantSICOPCheckbox => Page.Locator("input[type=checkbox][name=IsCompliantSICOP]");
    public ILocator PriceInput => Page.Locator("[name=Price]");
    public ILocator ValidUntilInput => Page.Locator("[name=ValidUntil]");
    public ILocator QuotationFileInput => Page.Locator("[name=QuotationFile]");
    public ILocator SubmitButton => Page.Locator("main button[type=submit]");
    public ILocator ValidationSummary => Page.Locator(".text-danger");

    public async Task NavigateToAddAsync(int appId, int itemId, string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Application/{appId}/Item/{itemId}/Supplier/Add");
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
