using System.Text.RegularExpressions;
using FundingPlatform.Tests.E2E.Fixtures;
using FundingPlatform.Tests.E2E.PageObjects;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace FundingPlatform.Tests.E2E.Tests;

/// <summary>
/// End-to-end tests for spec 007 Amendment 1, SC-010 (Generate Agreement Queue tab).
///
/// The Aspire host uses <c>.WithDataVolume("fundingplatform-sqldata")</c> which persists
/// SQL data across test runs on the same machine.  Tests 2 and 3 seed ResponseFinalized
/// apps that remain in the queue between runs.  Test 1 (Order 1) therefore clears any
/// lingering queue entries via <see cref="SeedAgreementsForExistingQueueEntriesAsync"/>
/// before asserting the empty-state element, ensuring a clean baseline on every run.
///
/// Note: <c>ApplicationState.ResponseFinalized = 5</c> (not 4 which is AppealOpen).
/// </summary>
[TestFixture]
[Category("GenerateAgreementQueue")]
public class GenerateAgreementQueueTests : AuthenticatedTestBase
{
    private string _quotationFilePath = string.Empty;
    private readonly List<string> _seededFiles = [];
    private const string Password = "Test123!";

    [SetUp]
    public void SetUp()
    {
        _quotationFilePath = Path.Combine(Path.GetTempPath(), $"ga-quote-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(_quotationFilePath, "Quotation placeholder content");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_quotationFilePath)) File.Delete(_quotationFilePath);
        foreach (var path in _seededFiles)
        {
            if (File.Exists(path)) File.Delete(path);
        }
        _seededFiles.Clear();
    }

    /// <summary>
    /// SC-010-A: A reviewer with no matching apps sees the empty-state message.
    ///
    /// <see cref="SeedAgreementsForExistingQueueEntriesAsync"/> clears leftover
    /// ResponseFinalized apps from prior runs before the reviewer navigates to the page.
    /// </summary>
    [Test, Order(1)]
    public async Task GenerateAgreementTab_Empty_ShowsEmptyState()
    {
        await SeedAgreementsForExistingQueueEntriesAsync();

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var reviewerEmail = $"ga_empty_{uniqueId}@example.com";
        await RegisterUserAsync(Page, reviewerEmail, Password, "GA", "EmptyReviewer", $"GAE-{uniqueId}");
        await AssignRoleAsync(reviewerEmail, "Reviewer");
        await LoginAsync(Page, reviewerEmail, Password);

        var queuePage = new ReviewQueuePage(Page);
        await queuePage.GotoGenerateAgreementAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(queuePage.GenerateAgreementEmpty).ToBeVisibleAsync();
        await Expect(queuePage.GenerateAgreementEmpty)
            .ToHaveTextAsync("No applications are waiting for agreement generation.");
        Assert.That(await queuePage.IsGenerateAgreementTabActive(), Is.True,
            "Generate Agreement tab should be marked active on its own route.");
    }

    /// <summary>
    /// SC-010-B: A ResponseFinalized application without an agreement appears
    /// in the queue for both Reviewer and Admin roles.
    /// </summary>
    [Test, Order(2)]
    public async Task ReviewerAndAdmin_BothSee_ResponseFinalizedApplicationWithoutAgreement()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var (applicationId, _, _) = await CreateApplicationAndSubmitResponseAsync(uniqueId, _quotationFilePath);

        // Reviewer #2 (separate from the seeding reviewer) sees the app in the queue
        var reviewerEmail = $"ga_rev_{uniqueId}@example.com";
        await RegisterUserAsync(Page, reviewerEmail, Password, "GA", "Reviewer", $"GAR-{uniqueId}");
        await AssignRoleAsync(reviewerEmail, "Reviewer");
        await LoginAsync(Page, reviewerEmail, Password);

        var queuePage = new ReviewQueuePage(Page);
        await queuePage.GotoAsync(BaseUrl);
        await queuePage.ClickGenerateAgreementTab();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(queuePage.GenerateAgreementTable).ToBeVisibleAsync();
        await Expect(Page.Locator(
            $"[data-testid=generate-agreement-row][data-application-id='{applicationId}']")).ToBeVisibleAsync();

        // Logout and re-login as admin
        await Page.Locator("form[action*='Account/Logout'] button[type=submit]").ClickAsync();

        var adminEmail = $"ga_admin_{uniqueId}@example.com";
        await RegisterUserAsync(Page, adminEmail, Password, "GA", "Admin", $"GAA-{uniqueId}");
        await AssignRoleAsync(adminEmail, "Admin");
        await LoginAsync(Page, adminEmail, Password);

        await queuePage.GotoAsync(BaseUrl);
        await queuePage.ClickGenerateAgreementTab();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(queuePage.GenerateAgreementTable).ToBeVisibleAsync();
        await Expect(Page.Locator(
            $"[data-testid=generate-agreement-row][data-application-id='{applicationId}']")).ToBeVisibleAsync();
    }

    /// <summary>
    /// SC-010-C: Full chain — reviewer navigates from the queue to the app's
    /// agreement page; after an agreement is generated the app leaves the queue;
    /// the applicant's response page shows the "ready to sign" banner.
    ///
    /// Note: PDF generation via Syncfusion is not available in the test environment
    /// (same constraint as <see cref="SigningWayfindingTests"/>), so the agreement
    /// is seeded directly via <see cref="FundingAgreementSeeder"/> after the
    /// reviewer has navigated to the agreement page.  The queue-departure and
    /// banner assertions are unaffected by this bypass.
    /// </summary>
    [Test, Order(3)]
    public async Task EndToEndChain_ReviewerGeneratesAgreement_ApplicantSeesReadyToSignBanner()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var (applicationId, applicantEmail, applicantPassword) =
            await CreateApplicationAndSubmitResponseAsync(uniqueId, _quotationFilePath);

        var reviewerEmail = $"ga_chain_{uniqueId}@example.com";
        var adminEmail = $"ga_chain_admin_{uniqueId}@example.com";
        await RegisterUserAsync(Page, adminEmail, Password, "GA", "ChainAdmin", $"GACA-{uniqueId}");
        await AssignRoleAsync(adminEmail, "Admin");
        await RegisterUserAsync(Page, reviewerEmail, Password, "GA", "ChainReviewer", $"GAC-{uniqueId}");
        await AssignRoleAsync(reviewerEmail, "Reviewer");
        await LoginAsync(Page, reviewerEmail, Password);

        var queuePage = new ReviewQueuePage(Page);
        await queuePage.GotoAsync(BaseUrl);
        await queuePage.ClickGenerateAgreementTab();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the app appears in the queue, then click Open
        await Expect(Page.Locator(
            $"[data-testid=generate-agreement-row][data-application-id='{applicationId}']")).ToBeVisibleAsync();
        await Page.Locator(
            $"[data-testid=generate-agreement-row][data-application-id='{applicationId}'] a:has-text('Open')")
            .ClickAsync();

        Assert.That(Regex.IsMatch(Page.Url, $@"/Applications/{applicationId}/FundingAgreement"), Is.True,
            $"Expected to land on funding-agreement details page; was at {Page.Url}");

        // Seed the agreement via SQL (bypasses Syncfusion — same approach as SigningWayfindingTests).
        // This simulates what "Generate agreement" would do in a licensed environment.
        var seededPdf = await FundingAgreementSeeder.SeedGeneratedAgreementAsync(
            ConnectionString, applicationId, adminEmail);
        _seededFiles.Add(seededPdf);

        // Reload the page and confirm the Download link is now present (agreement seeded)
        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("[data-testid=funding-agreement-download]")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

        // The application should now leave the Generate Agreement tab
        // (queue filter: State == ResponseFinalized AND FundingAgreement IS NULL)
        await queuePage.GotoAsync(BaseUrl);
        await queuePage.ClickGenerateAgreementTab();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator(
            $"[data-testid=generate-agreement-row][data-application-id='{applicationId}']")).ToHaveCountAsync(0);

        // Log back in as the applicant and confirm the ready-to-sign banner
        await Page.Locator("form[action*='Account/Logout'] button[type=submit]").ClickAsync();
        await LoginAsync(Page, applicantEmail, applicantPassword);
        await Page.GotoAsync($"{BaseUrl}/ApplicantResponse/Index/{applicationId}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("[data-testid=signing-banner-ready]")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Clears leftover "pending agreement" queue entries produced by previous test
    /// runs on the persistent Aspire SQL volume (<c>fundingplatform-sqldata</c>).
    /// Inserts a placeholder <c>FundingAgreements</c> row for every
    /// ResponseFinalized app (State=5) that has no agreement yet, so the web app's
    /// queue query returns zero rows when Test 1 runs.
    /// </summary>
    private async Task SeedAgreementsForExistingQueueEntriesAsync()
    {
        var pdfPath = Path.Combine(Path.GetTempPath(), $"ga-clear-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(pdfPath,
            System.Text.Encoding.UTF8.GetBytes("%PDF-1.4\nqueue-clear placeholder\n%%EOF\n"));
        _seededFiles.Add(pdfPath);

        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        // ApplicationState.ResponseFinalized = 5 (AppealOpen = 4, AgreementExecuted = 6)
        const string clearQueueSql = @"
INSERT INTO dbo.FundingAgreements
    (ApplicationId, FileName, ContentType, Size, StoragePath, GeneratedAtUtc, GeneratedByUserId, GeneratedVersion)
SELECT
    a.Id,
    'FundingAgreement-clear-' + CAST(a.Id AS VARCHAR) + '.pdf',
    'application/pdf',
    42,
    @pdfPath,
    GETUTCDATE(),
    ap.UserId,
    1
FROM dbo.Applications a
JOIN dbo.Applicants ap ON ap.Id = a.ApplicantId
WHERE a.State = 5
  AND NOT EXISTS (SELECT 1 FROM dbo.FundingAgreements fa WHERE fa.ApplicationId = a.Id);";

        using var cmd = new SqlCommand(clearQueueSql, conn);
        cmd.Parameters.AddWithValue("@pdfPath", pdfPath);
        await cmd.ExecuteNonQueryAsync();
    }
}
