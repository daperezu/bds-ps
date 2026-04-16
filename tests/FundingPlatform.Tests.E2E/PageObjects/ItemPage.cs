using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class ItemPage
{
    private readonly IPage _page;

    public ItemPage(IPage page)
    {
        _page = page;
    }

    public ILocator ProductNameInput => _page.Locator("[name=ProductName]");
    public ILocator CategorySelect => _page.Locator("[name=CategoryId]");
    public ILocator TechnicalSpecificationsInput => _page.Locator("[name=TechnicalSpecifications]");
    public ILocator SubmitButton => _page.Locator("main button[type=submit]");
    public ILocator ValidationSummary => _page.Locator(".text-danger");

    public async Task AddItemAsync(int appId, string productName, int categoryIndex, string techSpecs, string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Application/{appId}/Item/Add");
        await ProductNameInput.FillAsync(productName);

        var options = await CategorySelect.Locator("option").AllAsync();
        if (options.Count > categoryIndex + 1) // +1 to skip the "-- Select Category --" option
        {
            var value = await options[categoryIndex + 1].GetAttributeAsync("value");
            if (value is not null)
            {
                await CategorySelect.SelectOptionAsync(value);
            }
        }

        await TechnicalSpecificationsInput.FillAsync(techSpecs);
        await SubmitButton.ClickAsync();
    }

    public async Task EditItemAsync(int appId, int itemId, string productName, int categoryIndex, string techSpecs, string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Application/{appId}/Item/{itemId}/Edit");
        await ProductNameInput.ClearAsync();
        await ProductNameInput.FillAsync(productName);

        var options = await CategorySelect.Locator("option").AllAsync();
        if (options.Count > categoryIndex + 1)
        {
            var value = await options[categoryIndex + 1].GetAttributeAsync("value");
            if (value is not null)
            {
                await CategorySelect.SelectOptionAsync(value);
            }
        }

        await TechnicalSpecificationsInput.ClearAsync();
        await TechnicalSpecificationsInput.FillAsync(techSpecs);
        await SubmitButton.ClickAsync();
    }
}
