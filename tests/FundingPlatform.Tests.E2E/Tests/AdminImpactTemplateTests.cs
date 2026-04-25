using System.Text.RegularExpressions;
using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.PageObjects;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests;

public class AdminImpactTemplateTests : AuthenticatedTestBase
{
    private async Task RegisterAndLoginAsAdminAsync(IPage page, string email, string password)
    {
        // Register as a regular user
        await RegisterUserAsync(page, email, password, "Admin", "Tester", $"LID-{Guid.NewGuid():N}"[..16]);

        // Promote to admin via the development-only endpoint
        await page.GotoAsync($"{BaseUrl}/Account/Login");

        // Get the anti-forgery token from the form
        var token = await page.Locator("input[name='__RequestVerificationToken']").GetAttributeAsync("value");

        // Use the page's API request context to call the promote endpoint
        var formData = page.APIRequest.CreateFormData();
        formData.Set("email", email);
        formData.Set("__RequestVerificationToken", token ?? "");

        var response = await page.APIRequest.PostAsync($"{BaseUrl}/Account/PromoteToAdmin", new APIRequestContextOptions
        {
            Form = formData
        });

        Assert.That(response.Ok, Is.True, "Failed to promote user to admin");

        // Login as the admin
        await LoginAsync(page, email, password);
    }

    [Test]
    public async Task CreateTemplate_Successfully()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"admin_create_{uniqueId}@example.com";
        var password = "Test123!";
        var templateName = $"Test Template {uniqueId}";

        await RegisterAndLoginAsAdminAsync(Page, email, password);

        var adminPage = new AdminPage(Page);

        // Navigate to admin dashboard
        await adminPage.GotoDashboardAsync(BaseUrl);
        await Expect(Page).ToHaveURLAsync(new Regex("/Admin$"));

        // Go to Impact Templates
        await adminPage.ManageTemplatesLink.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Admin/ImpactTemplates"));

        // Click Create New Template
        await adminPage.CreateNewTemplateButton.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Admin/CreateTemplate"));

        // Fill template name and description
        await adminPage.TemplateNameInput.FillAsync(templateName);
        await adminPage.TemplateDescriptionInput.FillAsync("A test template created by E2E tests");

        // Add a parameter
        await adminPage.AddParameterButton.ClickAsync();
        await adminPage.FillParameterAsync(0, "beneficiaries", "Number of Beneficiaries", "Integer", true, 0);

        // Add a second parameter
        await adminPage.AddParameterButton.ClickAsync();
        await adminPage.FillParameterAsync(1, "description", "Impact Description", "Text", false, 1);

        // Submit the form
        await adminPage.SubmitButton.ClickAsync();

        // Should redirect to templates list with success message
        await Expect(Page).ToHaveURLAsync(new Regex("/Admin/ImpactTemplates"));
        var successAlert = Page.Locator(".alert-success");
        await Expect(successAlert).ToBeVisibleAsync();

        // Verify the template appears in the table
        var templateRow = Page.Locator($"table tbody tr:has-text('{templateName}')");
        await Expect(templateRow).ToBeVisibleAsync();

        // Verify parameter count shows 2
        var paramCountCell = templateRow.Locator("td:nth-child(3)");
        await Expect(paramCountCell).ToHaveTextAsync("2");

        // Verify status is Active
        var activeBadge = templateRow.Locator(".status:has-text('Active')");
        await Expect(activeBadge).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditTemplate_UpdatesSuccessfully()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"admin_edit_{uniqueId}@example.com";
        var password = "Test123!";
        var templateName = $"Edit Template {uniqueId}";
        var updatedName = $"Updated Template {uniqueId}";

        await RegisterAndLoginAsAdminAsync(Page, email, password);

        var adminPage = new AdminPage(Page);

        // First create a template
        await adminPage.GotoCreateTemplateAsync(BaseUrl);
        await adminPage.TemplateNameInput.FillAsync(templateName);
        await adminPage.TemplateDescriptionInput.FillAsync("Original description");
        await adminPage.AddParameterButton.ClickAsync();
        await adminPage.FillParameterAsync(0, "count", "Count", "Integer", true, 0);
        await adminPage.SubmitButton.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Admin/ImpactTemplates"));

        // Click Edit on the template we just created
        var templateRow = Page.Locator($"table tbody tr:has-text('{templateName}')");
        await Expect(templateRow).ToBeVisibleAsync();
        var editButton = templateRow.Locator("a:has-text('Edit')");
        await editButton.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/Admin/EditTemplate/\\d+"));

        // Update the template name
        await adminPage.TemplateNameInput.ClearAsync();
        await adminPage.TemplateNameInput.FillAsync(updatedName);

        // Update description
        await adminPage.TemplateDescriptionInput.ClearAsync();
        await adminPage.TemplateDescriptionInput.FillAsync("Updated description");

        // Submit the form
        await adminPage.SubmitButton.ClickAsync();

        // Should redirect to templates list with success message
        await Expect(Page).ToHaveURLAsync(new Regex("/Admin/ImpactTemplates"));
        var successAlert = Page.Locator(".alert-success");
        await Expect(successAlert).ToBeVisibleAsync();

        // Verify the updated template name appears
        var updatedRow = Page.Locator($"table tbody tr:has-text('{updatedName}')");
        await Expect(updatedRow).ToBeVisibleAsync();

        // Verify the old name is gone
        var oldRow = Page.Locator($"table tbody tr:has-text('{templateName}')");
        await Expect(oldRow).ToHaveCountAsync(0);
    }
}
