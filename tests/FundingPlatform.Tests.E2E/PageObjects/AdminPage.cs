using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class AdminPage
{
    private readonly IPage _page;

    public AdminPage(IPage page)
    {
        _page = page;
    }

    // Dashboard
    public ILocator ManageTemplatesLink => _page.Locator("a:has-text('Manage Templates')");
    public ILocator ManageConfigurationLink => _page.Locator("a:has-text('Manage Configuration')");

    // Impact Templates list
    public ILocator CreateNewTemplateButton => _page.Locator("a:has-text('Create New Template')");
    public ILocator TemplatesTable => _page.Locator("table");
    public ILocator TemplateRows => _page.Locator("table tbody tr");

    // Create/Edit Template form
    public ILocator TemplateNameInput => _page.Locator("[name=Name]");
    public ILocator TemplateDescriptionInput => _page.Locator("[name=Description]");
    public ILocator IsActiveCheckbox => _page.Locator("[name=IsActive]");
    public ILocator AddParameterButton => _page.Locator("#addParameter");
    public ILocator SubmitButton => _page.Locator("main button[type=submit]");
    public ILocator ParameterRows => _page.Locator(".parameter-row");

    // Configuration
    public ILocator ConfigurationTable => _page.Locator("table");
    public ILocator SaveConfigurationButton => _page.Locator("button[type=submit]:has-text('Save Configuration')");

    public async Task GotoDashboardAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Admin");
    }

    public async Task GotoImpactTemplatesAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Admin/ImpactTemplates");
    }

    public async Task GotoCreateTemplateAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Admin/CreateTemplate");
    }

    public async Task GotoEditTemplateAsync(string baseUrl, int id)
    {
        await _page.GotoAsync($"{baseUrl}/Admin/EditTemplate/{id}");
    }

    public async Task GotoConfigurationAsync(string baseUrl)
    {
        await _page.GotoAsync($"{baseUrl}/Admin/Configuration");
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
