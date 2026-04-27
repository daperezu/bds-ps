using Microsoft.AspNetCore.Mvc.Razor;

namespace FundingPlatform.Web.Identity;

public class AdminAreaViewLocationExpander : IViewLocationExpander
{
    private const string AdminPrefix = "Admin";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        var controller = context.ControllerName;
        if (string.IsNullOrEmpty(controller)
            || !controller.StartsWith(AdminPrefix, StringComparison.Ordinal)
            || controller.Length == AdminPrefix.Length)
        {
            return viewLocations;
        }

        var subfolder = controller.Substring(AdminPrefix.Length);
        var extra = new[]
        {
            $"/Views/Admin/{subfolder}/{{0}}.cshtml",
        };
        return extra.Concat(viewLocations);
    }
}
