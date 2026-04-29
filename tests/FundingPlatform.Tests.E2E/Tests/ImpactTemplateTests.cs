using System.Text.RegularExpressions;
using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.PageObjects;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests;

public class ImpactTemplateTests : AuthenticatedTestBase
{
    [Test]
    public async Task SelectTemplate_And_FillParameters()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"impact_test_{uniqueId}@example.com";
        var password = "Test123!";

        // Register and login
        await RegisterUserAsync(Page, email, password, "Impact", "Tester", $"LID-{uniqueId}");
        await LoginAsync(Page, email, password);

        // Create application
        var appPage = new ApplicationPage(Page);
        await appPage.GotoListAsync(BaseUrl);
        await appPage.CreateApplicationAsync();

        var url = Page.Url;
        var appIdMatch = Regex.Match(url, @"/Application/Details/(\d+)");
        Assert.That(appIdMatch.Success, Is.True, "Should be on application details page with ID");
        var appId = int.Parse(appIdMatch.Groups[1].Value);

        // Add an item
        var itemPage = new ItemPage(Page);
        await itemPage.AddItemAsync(appId, "Impact Test Laptop", 0, "Intel i7, 16GB RAM", BaseUrl);

        // Verify item appears
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/Details/\d+"));
        var itemRow = Page.Locator("table tbody tr:has-text('Impact Test Laptop')");
        await Expect(itemRow).ToBeVisibleAsync();

        // Verify impact badge shows "Pending"
        var pendingBadge = itemRow.Locator(".status:has-text('Pendiente')");
        await Expect(pendingBadge).ToBeVisibleAsync();

        // Click Impact button
        var impactButton = itemRow.Locator("a:has-text('Impacto')");
        await impactButton.ClickAsync();

        // Should be on impact page
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/\d+/Item/\d+/Impact"));

        // Select the first available template
        var templateSelector = Page.Locator("#templateSelector");
        await Expect(templateSelector).ToBeVisibleAsync();

        // Get first non-empty option
        var options = await templateSelector.Locator("option").AllAsync();
        Assert.That(options.Count, Is.GreaterThan(1), "Should have at least one template option");
        var firstOptionValue = await options[1].GetAttributeAsync("value");
        Assert.That(firstOptionValue, Is.Not.Null.And.Not.Empty);
        await templateSelector.SelectOptionAsync(firstOptionValue!);

        // Wait for parameters to load dynamically
        var parameterFields = Page.Locator(".parameter-field");
        await Expect(parameterFields.First).ToBeVisibleAsync();

        // Fill in all parameter fields
        var paramInputs = Page.Locator(".parameter-field input.form-control");
        var inputCount = await paramInputs.CountAsync();
        Assert.That(inputCount, Is.GreaterThan(0), "Should have parameter input fields");

        for (int i = 0; i < inputCount; i++)
        {
            var input = paramInputs.Nth(i);
            var inputType = await input.GetAttributeAsync("type");

            switch (inputType)
            {
                case "number":
                    await input.FillAsync("100");
                    break;
                case "date":
                    await input.FillAsync("2026-12-31");
                    break;
                default:
                    await input.FillAsync("Test value");
                    break;
            }
        }

        // Submit
        var submitButton = Page.Locator("button[type=submit]:has-text('Guardar impacto')");
        await submitButton.ClickAsync();

        // Should redirect back to application details
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/Details/\d+"));

        // Verify impact badge now shows "Complete"
        var updatedItemRow = Page.Locator("table tbody tr:has-text('Impact Test Laptop')");
        var completeBadge = updatedItemRow.Locator(".status:has-text('Complete')");
        await Expect(completeBadge).ToBeVisibleAsync();
    }

    [Test]
    public async Task RequiredParameter_Validation()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"impact_val_{uniqueId}@example.com";
        var password = "Test123!";

        // Register and login
        await RegisterUserAsync(Page, email, password, "Validate", "Tester", $"LID-{uniqueId}");
        await LoginAsync(Page, email, password);

        // Create application
        var appPage = new ApplicationPage(Page);
        await appPage.GotoListAsync(BaseUrl);
        await appPage.CreateApplicationAsync();

        var url = Page.Url;
        var appIdMatch = Regex.Match(url, @"/Application/Details/(\d+)");
        var appId = int.Parse(appIdMatch.Groups[1].Value);

        // Add an item
        var itemPage = new ItemPage(Page);
        await itemPage.AddItemAsync(appId, "Validation Test Item", 0, "Test specs", BaseUrl);

        // Click Impact button
        var itemRow = Page.Locator("table tbody tr:has-text('Validation Test Item')");
        var impactButton = itemRow.Locator("a:has-text('Impacto')");
        await impactButton.ClickAsync();

        // Select a template
        var templateSelector = Page.Locator("#templateSelector");
        var options = await templateSelector.Locator("option").AllAsync();
        Assert.That(options.Count, Is.GreaterThan(1), "Should have at least one template option");
        var firstOptionValue = await options[1].GetAttributeAsync("value");
        await templateSelector.SelectOptionAsync(firstOptionValue!);

        // Wait for parameters to load
        var parameterFields = Page.Locator(".parameter-field");
        await Expect(parameterFields.First).ToBeVisibleAsync();

        // Do NOT fill in any parameters — leave them empty

        // Try to submit the form
        var submitButton = Page.Locator("button[type=submit]:has-text('Guardar impacto')");
        await submitButton.ClickAsync();

        // The browser's built-in validation should prevent submission for required fields.
        // Verify we are still on the Impact page (form was not submitted).
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/\d+/Item/\d+/Impact"));
    }
}
