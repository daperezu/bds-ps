using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace FundingPlatform.Tests.E2E.Fixtures;

public class AuthenticatedTestBase : PageTest
{
    private static readonly AspireFixture _fixture = new();
    private static bool _initialized;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    protected string BaseUrl => _fixture.BaseUrl;

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        };
    }

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await _initLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await _fixture.StartAsync();
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    protected async Task RegisterUserAsync(IPage page, string email, string password, string firstName, string lastName, string legalId)
    {
        await page.GotoAsync($"{BaseUrl}/Account/Register");
        await page.FillAsync("[name=Email]", email);
        await page.FillAsync("[name=Password]", password);
        await page.FillAsync("[name=ConfirmPassword]", password);
        await page.FillAsync("[name=FirstName]", firstName);
        await page.FillAsync("[name=LastName]", lastName);
        await page.FillAsync("[name=LegalId]", legalId);
        await page.Locator("form[action*='Account/Register'] button[type=submit]").ClickAsync();
    }

    protected async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{BaseUrl}/Account/Login");
        await page.FillAsync("[name=Email]", email);
        await page.FillAsync("[name=Password]", password);
        await page.Locator("form[action*='Account/Login'] button[type=submit]").ClickAsync();
    }
}

[SetUpFixture]
public class GlobalTeardown
{
    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        // Aspire host is cleaned up when the process exits.
        // We don't dispose mid-run because the fixture is shared across test classes.
    }
}
