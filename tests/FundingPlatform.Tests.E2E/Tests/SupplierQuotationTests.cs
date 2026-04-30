using System.Text.RegularExpressions;
using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.PageObjects;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests;

public class SupplierQuotationTests : AuthenticatedTestBase
{
    private string _testFilePath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        // Create a temporary test file for upload
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test-quotation-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(_testFilePath, "Test quotation document content");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Test]
    public async Task AddSupplier_WithQuotation_Successfully()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"supplier_test_{uniqueId}@example.com";
        var password = "Test123!";

        // Register and login
        await RegisterUserAsync(Page, email, password, "Supplier", "Tester", $"LID-{uniqueId}");
        await LoginAsync(Page, email, password);

        // Create application
        var appPage = new ApplicationPage(Page);
        await appPage.GotoListAsync(BaseUrl);
        await appPage.CreateApplicationAsync();

        var url = Page.Url;
        var appIdMatch = Regex.Match(url, @"/Application/Details/(\d+)");
        Assert.That(appIdMatch.Success, Is.True, "Should be on application details page with ID");
        var appId = int.Parse(appIdMatch.Groups[1].Value);

        // Add an item first
        var itemPage = new ItemPage(Page);
        await itemPage.AddItemAsync(appId, "Server Equipment", 0, "High-performance server", BaseUrl);

        // Should redirect back to application details
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/Details/\d+"));

        // Extract item ID from the items table - find the "Add Supplier" link
        var addSupplierLink = Page.Locator("a:has-text('Agregar proveedor')").First;
        await Expect(addSupplierLink).ToBeVisibleAsync();
        await addSupplierLink.ClickAsync();

        // Fill in supplier form
        var supplierPage = new SupplierPage(Page);
        await supplierPage.FillSupplierFormAsync(
            legalId: $"SUP-{uniqueId}",
            name: "Test Supplier Corp",
            price: 1500.00m,
            validUntil: "2027-12-31",
            filePath: _testFilePath,
            contactName: "John Doe",
            email: "supplier@test.com",
            phone: "555-0100",
            location: "San Jose");
        await supplierPage.SubmitAsync();

        // Should redirect back to application details
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/Details/\d+"));

        // Verify quotation count increased
        var itemRow = Page.Locator("table tbody tr:has-text('Server Equipment')");
        await Expect(itemRow).ToBeVisibleAsync();
    }

    [Test]
    public async Task DuplicateSupplier_ShowsError()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var email = $"dup_supplier_{uniqueId}@example.com";
        var password = "Test123!";
        var supplierLegalId = $"SUP-DUP-{uniqueId}";

        // Register and login
        await RegisterUserAsync(Page, email, password, "Dup", "Tester", $"LID-{uniqueId}");
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
        await itemPage.AddItemAsync(appId, "Network Switch", 0, "48-port managed switch", BaseUrl);

        // Add first supplier successfully
        var addSupplierLink = Page.Locator("a:has-text('Agregar proveedor')").First;
        await Expect(addSupplierLink).ToBeVisibleAsync();
        await addSupplierLink.ClickAsync();

        var supplierPage = new SupplierPage(Page);
        await supplierPage.FillSupplierFormAsync(
            legalId: supplierLegalId,
            name: "Duplicate Supplier",
            price: 2000.00m,
            validUntil: "2027-12-31",
            filePath: _testFilePath);
        await supplierPage.SubmitAsync();

        // Should redirect back to details
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/Details/\d+"));

        // Try adding same supplier again
        var addSupplierLink2 = Page.Locator("a:has-text('Agregar proveedor')").First;
        await addSupplierLink2.ClickAsync();

        await supplierPage.FillSupplierFormAsync(
            legalId: supplierLegalId,
            name: "Duplicate Supplier",
            price: 2500.00m,
            validUntil: "2027-12-31",
            filePath: _testFilePath);
        await supplierPage.SubmitAsync();

        // Should see an error message about duplicate supplier
        var errorMessage = Page.Locator(".validation-summary-errors li, [data-valmsg-summary] li");
        await Expect(errorMessage.First).ToBeVisibleAsync();
    }
}
