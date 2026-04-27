using FundingPlatform.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FundingPlatform.Web.Middleware;

public class MustChangePasswordMiddleware
{
    private static readonly string[] AllowedSuffixes =
        [".css", ".js", ".map", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".woff", ".woff2", ".ico"];

    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        var userId = userManager.GetUserId(context.User);
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is { MustChangePassword: true, IsSystemSentinel: false })
        {
            context.Response.Redirect("/Account/ChangePassword");
            return;
        }

        await _next(context);
    }

    private static bool IsExcludedPath(PathString path)
    {
        if (!path.HasValue) return false;
        var value = path.Value!;
        if (value.Equals("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("/Account/Logout", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/lib", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/img", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var suffix in AllowedSuffixes)
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
