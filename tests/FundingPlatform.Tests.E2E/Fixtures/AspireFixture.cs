using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace FundingPlatform.Tests.E2E.Fixtures;

public class AspireFixture : IAsyncDisposable
{
    private DistributedApplication? _app;
    public string BaseUrl { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.FundingPlatform_AppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        await DeployDacpacAsync();

        // Use http — the test environment may not trust the dev HTTPS certificate
        var webapp = _app.GetEndpoint("webapp", "http");
        BaseUrl = webapp.ToString().TrimEnd('/');

        // Verify the web app is actually responding
        await WaitForWebAppAsync();
    }

    private async Task WaitForWebAppAsync()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        for (var i = 0; i < 30; i++)
        {
            try
            {
                var response = await client.GetAsync("/");
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Found)
                    return;
            }
            catch (HttpRequestException)
            {
                // Web app not ready yet
            }
            await Task.Delay(1000);
        }

        throw new TimeoutException($"Web app at {BaseUrl} did not become healthy within 30 seconds");
    }

    private async Task DeployDacpacAsync()
    {
        if (_app is null) throw new InvalidOperationException("App not started");

        var connectionString = await _app.GetConnectionStringAsync("fundingdb");

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Could not resolve 'fundingdb' connection string from Aspire host");

        var dacpacPath = FindDacpac();

        var sqlpackagePath = FindOnPath("sqlpackage")
            ?? throw new FileNotFoundException(
                "sqlpackage not found. Install it with: dotnet tool install -g microsoft.sqlpackage");

        var psi = new ProcessStartInfo
        {
            FileName = sqlpackagePath,
            Arguments = string.Join(" ",
                "/Action:Publish",
                $"/SourceFile:\"{dacpacPath}\"",
                $"/TargetConnectionString:\"{connectionString}\"",
                "/p:VerifyDeployment=false"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Ensure DOTNET_ROOT is set so sqlpackage can find the .NET runtime
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");
        if (Directory.Exists(dotnetRoot))
        {
            psi.Environment["DOTNET_ROOT"] = dotnetRoot;
        }

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start sqlpackage");

        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                $"sqlpackage failed (exit {proc.ExitCode}):\n{stderr}\n{stdout}");
    }

    private static string FindDacpac()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FundingPlatform.slnx")))
            dir = dir.Parent;

        if (dir is null)
            throw new FileNotFoundException("Could not find solution root (FundingPlatform.slnx)");

        var dacpac = Path.Combine(dir.FullName,
            "src", "FundingPlatform.Database", "bin", "Debug", "FundingPlatform.Database.dacpac");

        if (!File.Exists(dacpac))
            throw new FileNotFoundException($"Dacpac not found at {dacpac}. Run 'dotnet build src/FundingPlatform.Database' first.");

        return dacpac;
    }

    private static string? FindOnPath(string executable)
    {
        // Check well-known .NET tools directory first
        var dotnetToolsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        var dotnetToolPath = Path.Combine(dotnetToolsDir, executable);
        if (File.Exists(dotnetToolPath))
            return dotnetToolPath;

        // Fall back to PATH search
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(dir, executable);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
