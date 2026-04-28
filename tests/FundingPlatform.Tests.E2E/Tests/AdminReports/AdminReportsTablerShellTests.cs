using System.Text.RegularExpressions;

namespace FundingPlatform.Tests.E2E.Tests.AdminReports;

[Category("AdminReports")]
[Category("Static")]
public class AdminReportsTablerShellTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FundingPlatform.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not find repo root.");
    }

    private static IEnumerable<string> ReportViews()
    {
        var root = FindRepoRoot();
        var dir = Path.Combine(root, "src", "FundingPlatform.Web", "Views", "Admin", "Reports");
        if (!Directory.Exists(dir)) yield break;
        foreach (var path in Directory.EnumerateFiles(dir, "*.cshtml", SearchOption.AllDirectories))
        {
            yield return path;
        }
    }

    [Test]
    public void Reports_NoInlineStyleAttributes()
    {
        var rx = new Regex("\\bstyle=\"", RegexOptions.Compiled);
        foreach (var path in ReportViews())
        {
            var content = File.ReadAllText(path);
            Assert.That(rx.IsMatch(content), Is.False,
                $"Report view '{Path.GetFileName(path)}' must not use inline style attributes (FR-028).");
        }
    }

    [Test]
    public void Reports_NoBadgeMarkupOutsideStatusPill()
    {
        var rx = new Regex("<span class=\"badge", RegexOptions.Compiled);
        foreach (var path in ReportViews())
        {
            var content = File.ReadAllText(path);
            Assert.That(rx.IsMatch(content), Is.False,
                $"Report view '{Path.GetFileName(path)}' must not introduce raw badge markup; use _StatusPill.");
        }
    }

    [Test]
    public void KpiTilePartial_HasGenericParameterShape()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src", "FundingPlatform.Web", "Views", "Shared", "Components", "_KpiTile.cshtml");
        Assert.That(File.Exists(path), Is.True, "_KpiTile partial must exist.");
        var content = File.ReadAllText(path);
        Assert.That(content, Does.Contain("KpiTileViewModel"),
            "_KpiTile partial must consume the generic KpiTileViewModel.");
        Assert.That(content, Does.Not.Contain("admin-area"),
            "_KpiTile partial must remain admin-area-agnostic to be reusable elsewhere.");
    }
}
