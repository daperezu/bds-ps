using System.Text.RegularExpressions;
using FundingPlatform.Tests.E2E.Constants;
using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.PageObjects;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests;

public class AdminConfigurationTests : AuthenticatedTestBase
{
    private async Task RegisterAndLoginAsAdminAsync(IPage page, string email, string password)
    {
        // Register as a regular user
        await RegisterUserAsync(page, email, password, "Admin", "Tester", $"LID-{Guid.NewGuid():N}"[..16]);

        // Promote to admin via the development-only endpoint
        await page.GotoAsync($"{BaseUrl}/Account/Login");

        // Get the anti-forgery token from the form
        var token = await page.Locator("input[name='__RequestVerificationToken']").GetAttributeAsync("value");

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
    public async Task ViewConfiguration_ShowsSettings()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"admin_config_view_{uniqueId}@example.com";
        var password = "Test123!";

        await RegisterAndLoginAsAdminAsync(Page, email, password);

        var adminPage = new AdminPage(Page);

        // Navigate to configuration
        await adminPage.GotoConfigurationAsync(BaseUrl);
        await Expect(Page).ToHaveURLAsync(new Regex("/Admin/Configuration"));

        // The page should load without errors
        var heading = Page.Locator($"h2:has-text('{UiCopy.SystemConfiguration}')");
        await Expect(heading).ToBeVisibleAsync();
    }

    [Test]
    public async Task UpdateConfiguration_SavesSuccessfully()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"admin_config_update_{uniqueId}@example.com";
        var password = "Test123!";

        await RegisterAndLoginAsAdminAsync(Page, email, password);

        var adminPage = new AdminPage(Page);

        // Navigate to configuration
        await adminPage.GotoConfigurationAsync(BaseUrl);
        await Expect(Page).ToHaveURLAsync(new Regex("/Admin/Configuration"));

        // Check if there are any configuration rows
        var configInputs = Page.Locator("table tbody tr input.form-control");
        var inputCount = await configInputs.CountAsync();

        if (inputCount > 0)
        {
            // Modify the first configuration value
            var firstInput = configInputs.First;
            var originalValue = await firstInput.InputValueAsync();
            var newValue = $"test_value_{uniqueId}";
            await firstInput.ClearAsync();
            await firstInput.FillAsync(newValue);

            // Save
            await adminPage.SaveConfigurationButton.ClickAsync();

            // Should redirect with success message
            await Expect(Page).ToHaveURLAsync(new Regex("/Admin/Configuration"));
            var successAlert = Page.Locator(".alert-success");
            await Expect(successAlert).ToBeVisibleAsync();

            // Verify the value persisted
            var updatedInput = Page.Locator("table tbody tr input.form-control").First;
            await Expect(updatedInput).ToHaveValueAsync(newValue);

            // Restore original value
            await updatedInput.ClearAsync();
            await updatedInput.FillAsync(originalValue);
            await adminPage.SaveConfigurationButton.ClickAsync();
        }
        else
        {
            // No configurations exist, just verify the empty state message
            var emptyMessage = Page.Locator(".alert-info:has-text('No system configurations found')");
            await Expect(emptyMessage).ToBeVisibleAsync();
        }
    }
}
