using FundingPlatform.Tests.E2E.Constants;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class AdminPage : BasePage
{
    public AdminPage(IPage page) : base(page)
    {
    }

    // Dashboard
    public ILocator ManageTemplatesLink => Page.Locator($"a:has-text('{UiCopy.ManageTemplates}')");
    public ILocator ManageConfigurationLink => Page.Locator($"a:has-text('{UiCopy.ManageConfiguration}')");

    // Impact Templates list
    public ILocator CreateNewTemplateButton => Page.Locator($"a:has-text('{UiCopy.CreateNewTemplate}')");
    public ILocator TemplatesTable => Page.Locator("table");
    public ILocator TemplateRows => Page.Locator("table tbody tr");

    // Create/Edit Template form
    public ILocator TemplateNameInput => Page.Locator("[name=Name]");
    public ILocator TemplateDescriptionInput => Page.Locator("[name=Description]");
    public ILocator IsActiveCheckbox => Page.Locator("[name=IsActive]");
    public ILocator AddParameterButton => Page.Locator("#addParameter");
    public ILocator SubmitButton => Page.Locator("main button[type=submit]");
    public ILocator ParameterRows => Page.Locator(".parameter-row");

    // Configuration
    public ILocator ConfigurationTable => Page.Locator("table");
    public ILocator SaveConfigurationButton => Page.Locator($"button[type=submit]:has-text('{UiCopy.SaveConfiguration}')");

    public async Task GotoDashboardAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Admin");
    }

    public async Task GotoImpactTemplatesAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Admin/ImpactTemplates");
    }

    public async Task GotoCreateTemplateAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Admin/CreateTemplate");
    }

    public async Task GotoEditTemplateAsync(string baseUrl, int id)
    {
        await Page.GotoAsync($"{baseUrl}/Admin/EditTemplate/{id}");
    }

    public async Task GotoConfigurationAsync(string baseUrl)
    {
        await Page.GotoAsync($"{baseUrl}/Admin/Configuration");
    }

    public async Task FillParameterAsync(int index, string name, string displayLabel, string dataType, bool isRequired, int sortOrder)
    {
        var row = ParameterRows.Nth(index);
        await row.Locator($"[name='Parameters[{index}].Name']").FillAsync(name);
        await row.Locator($"[name='Parameters[{index}].DisplayLabel']").FillAsync(displayLabel);
        await row.Locator($"[name='Parameters[{index}].DataType']").SelectOptionAsync(dataType);
        if (isRequired)
        {
            await row.Locator($"[name='Parameters[{index}].IsRequired']").CheckAsync();
        }
        await row.Locator($"[name='Parameters[{index}].SortOrder']").FillAsync(sortOrder.ToString());
    }
}
