using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FundingPlatform.Web.Helpers;

/// <summary>
/// Razor helper for spec 011 (US7, FR-063). Inlines empty-state SVGs from
/// wwwroot/lib/illustrations/ so they inherit CSS custom properties and have
/// correct accessibility semantics out of the box.
/// </summary>
public static class IllustrationHelper
{
    public enum IllustrationSize { Sm, Md, Lg, Xl }

    private static readonly IReadOnlyDictionary<string, string> SceneToFile =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["seed"] = "seed.svg",
            ["folders-stack"] = "folders-stack.svg",
            ["open-envelope"] = "open-envelope.svg",
            ["connected-nodes"] = "connected-nodes.svg",
            ["calm-horizon"] = "calm-horizon.svg",
            ["soft-bar-chart"] = "soft-bar-chart.svg",
            ["off-center-compass"] = "off-center-compass.svg",
            ["gentle-disconnected-wires"] = "gentle-disconnected-wires.svg",
            ["magnifier-on-empty"] = "magnifier-on-empty.svg",
        };

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public static IHtmlContent Illustration(this IHtmlHelper html, string sceneKey)
        => Render(html, sceneKey, altText: null, IllustrationSize.Md);

    public static IHtmlContent Illustration(this IHtmlHelper html, string sceneKey, string altText)
        => Render(html, sceneKey, altText, IllustrationSize.Md);

    public static IHtmlContent Illustration(
        this IHtmlHelper html,
        string sceneKey,
        string? altText,
        IllustrationSize size)
        => Render(html, sceneKey, altText, size);

    private static IHtmlContent Render(IHtmlHelper html, string sceneKey, string? altText, IllustrationSize size)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            throw new ArgumentException("sceneKey is required.", nameof(sceneKey));
        }

        if (!SceneToFile.TryGetValue(sceneKey, out var filename))
        {
            throw new ArgumentException(
                $"Unknown illustration scene-key '{sceneKey}'. Known keys: {string.Join(", ", SceneToFile.Keys)}.",
                nameof(sceneKey));
        }

        var svg = LoadSvg(html, filename);
        if (svg is null)
        {
            // Asset not on disk yet — render an aria-hidden empty placeholder so the
            // page still renders. This is a graceful-degradation path; the
            // verify-illustrations.sh gate prevents shipping without all 9 SVGs.
            return new HtmlString(
                $"<span class=\"fl-empty-illustration\" data-size=\"{SizeAttr(size)}\" data-illustration-fallback=\"true\" aria-hidden=\"true\"></span>");
        }

        var sizeAttr = SizeAttr(size);
        var withAttrs = InjectRootAttributes(svg, altText, sizeAttr);
        return new HtmlString(withAttrs);
    }

    private static string SizeAttr(IllustrationSize size) => size switch
    {
        IllustrationSize.Sm => "sm",
        IllustrationSize.Lg => "lg",
        IllustrationSize.Xl => "xl",
        _ => "md",
    };

    private static string? LoadSvg(IHtmlHelper html, string filename)
    {
        if (Cache.TryGetValue(filename, out var cached)) return cached;

        var env = (IWebHostEnvironment?)html.ViewContext.HttpContext.RequestServices.GetService(typeof(IWebHostEnvironment));
        if (env is null) return null;

        var path = Path.Combine(env.WebRootPath, "lib", "illustrations", filename);
        if (!File.Exists(path)) return null;

        var content = File.ReadAllText(path);
        Cache.TryAdd(filename, content);
        return content;
    }

    private static string InjectRootAttributes(string svg, string? altText, string sizeAttr)
    {
        // Find the first '<svg' opening tag and extend its attribute list.
        var idx = svg.IndexOf("<svg", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return svg;
        var insertAt = svg.IndexOf('>', idx);
        if (insertAt < 0) return svg;

        var before = svg[..insertAt];
        var after = svg[insertAt..];

        // Strip any existing role/aria-* on the root tag — we are authoritative.
        before = StripAttribute(before, "role");
        before = StripAttribute(before, "aria-hidden");
        before = StripAttribute(before, "aria-label");
        before = StripAttribute(before, "class");

        var classAttr = $" class=\"fl-empty-illustration\" data-size=\"{sizeAttr}\"";
        string a11y;
        if (string.IsNullOrWhiteSpace(altText))
        {
            a11y = " aria-hidden=\"true\"";
        }
        else
        {
            var encoded = HtmlEncoder.Default.Encode(altText);
            a11y = $" role=\"img\" aria-label=\"{encoded}\"";
        }
        return before + classAttr + a11y + after;
    }

    private static string StripAttribute(string tag, string attr)
    {
        var lower = tag.ToLowerInvariant();
        var pos = 0;
        while (true)
        {
            var i = lower.IndexOf(" " + attr, pos, StringComparison.Ordinal);
            if (i < 0) return tag;
            // Quick disambiguation: ensure followed by '=' or whitespace.
            var post = i + 1 + attr.Length;
            if (post >= tag.Length) return tag;
            var ch = tag[post];
            if (ch != '=' && ch != ' ')
            {
                pos = post;
                continue;
            }
            // Find the end of the attribute value (handle ="..." or ='...' or bare).
            int end;
            if (ch == '=')
            {
                var quote = tag[post + 1];
                if (quote == '"' || quote == '\'')
                {
                    end = tag.IndexOf(quote, post + 2);
                    end = end < 0 ? tag.Length : end + 1;
                }
                else
                {
                    end = post + 1;
                    while (end < tag.Length && !char.IsWhiteSpace(tag[end])) end++;
                }
            }
            else
            {
                end = post;
            }
            tag = tag[..i] + tag[end..];
            lower = tag.ToLowerInvariant();
            pos = i;
        }
    }
}
