using System.Text.RegularExpressions;
using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.PageObjects;
using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.Tests;

public class ReviewApplicationTests : AuthenticatedTestBase
{
    private string _testFilePath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test-quotation-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(_testFilePath, "Test quotation document content");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Test]
    public async Task ReviewApplication_OpensSubmittedApp_TransitionsToUnderReview_DisplaysAllDetails()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var password = "Test123!";

        // Create and submit an application as an applicant
        var applicantEmail = $"ra_applicant_{uniqueId}@example.com";
        await RegisterUserAsync(Page, applicantEmail, password, "Review", "Applicant", $"LID-{uniqueId}");
        await LoginAsync(Page, applicantEmail, password);

        var appPage = new ApplicationPage(Page);
        await appPage.GotoListAsync(BaseUrl);
        await appPage.CreateApplicationAsync();

        var url = Page.Url;
        var appIdMatch = Regex.Match(url, @"/Application/Details/(\d+)");
        var appId = int.Parse(appIdMatch.Groups[1].Value);

        // Add an item
        var itemPage = new ItemPage(Page);
        await itemPage.AddItemAsync(appId, "Review Detail Item", 0, "Intel i9, 32GB RAM", BaseUrl);

        // Add two suppliers with different prices
        var supplierPage = new SupplierPage(Page);
        var addSupplierLink = Page.Locator("a:has-text('Agregar proveedor')").First;
        await addSupplierLink.ClickAsync();
        await supplierPage.FillSupplierFormAsync($"SUP1-{uniqueId}", "Cheap Supplier", 800m, "2027-12-31", _testFilePath,
            contactName: "Contact One", email: "sup1@test.com");
        await supplierPage.SubmitAsync();

        addSupplierLink = Page.Locator("a:has-text('Agregar proveedor')").First;
        await addSupplierLink.ClickAsync();
        await supplierPage.FillSupplierFormAsync($"SUP2-{uniqueId}", "Expensive Supplier", 1500m, "2027-12-31", _testFilePath,
            contactName: "Contact Two", email: "sup2@test.com");
        await supplierPage.SubmitAsync();

        // Set impact assessment
        var impactButton = Page.Locator("a:has-text('Impacto')").First;
        await impactButton.ClickAsync();
        await PickFirstImpactTemplateAsync();
        var paramInputs = Page.Locator(".parameter-field input.form-control");
        var inputCount = await paramInputs.CountAsync();
        for (int i = 0; i < inputCount; i++)
        {
            var input = paramInputs.Nth(i);
            var inputType = await input.GetAttributeAsync("type");
            await input.FillAsync(inputType == "number" ? "100" : inputType == "date" ? "2026-12-31" : "Test value");
        }
        await Page.Locator("button[type=submit]:has-text('Guardar impacto')").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/Application/Details/\d+"));

        // Submit the application
        await Page.Locator("button[type=submit]:has-text('Enviar solicitud')").ClickAsync();
        await Expect(Page.Locator("[data-testid=status-pill]:has-text('Enviada')")).ToBeVisibleAsync();

        // Logout and login as reviewer
        await Page.Locator("form[action*='Account/Logout'] button[type=submit]").ClickAsync();

        var reviewerEmail = $"ra_reviewer_{uniqueId}@example.com";
        await RegisterUserAsync(Page, reviewerEmail, password, "Detail", "Reviewer", $"RLID-{uniqueId}");
        await AssignRoleAsync(reviewerEmail, "Reviewer");
        await LoginAsync(Page, reviewerEmail, password);

        // Navigate to review the application
        var reviewAppPage = new ReviewApplicationPage(Page);
        await reviewAppPage.GotoAsync(BaseUrl, appId);

        // Verify application state shows Under Review
        await Expect(reviewAppPage.ApplicationState).ToContainTextAsync("En revisión");

        // Verify applicant info is displayed
        await Expect(reviewAppPage.ApplicantName).ToContainTextAsync("Review Applicant");

        // Verify items are displayed
        var itemCards = reviewAppPage.ItemCards;
        await Expect(itemCards).ToHaveCountAsync(1);

        // Verify item details contain the product name, tech specs
        var firstItem = itemCards.First;
        await Expect(firstItem).ToContainTextAsync("Review Detail Item");
        await Expect(firstItem).ToContainTextAsync("Intel i9, 32GB RAM");

        // Verify quotation/supplier info is displayed
        await Expect(firstItem).ToContainTextAsync("Cheap Supplier");
        await Expect(firstItem).ToContainTextAsync("Expensive Supplier");
    }
}
