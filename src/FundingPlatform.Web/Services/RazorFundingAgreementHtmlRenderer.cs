using FundingPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace FundingPlatform.Web.Services;

public class RazorFundingAgreementHtmlRenderer : IFundingAgreementHtmlRenderer
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public RazorFundingAgreementHtmlRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RenderAsync<TModel>(string viewPath, TModel model)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var routeData = new RouteData();

        // Razor's default ViewLocationFormats substitute {1} = controller. When this
        // renderer runs outside an MVC pipeline the value is otherwise empty, so any
        // relative partial or layout reference (e.g. "Partials/_Foo", "_Layout") falls
        // back to /Views/Shared and misses sibling files under /Views/{controller}/.
        // Derive the controller from a "~/Views/{controller}/..." viewPath so partials
        // and layouts resolve the same way they would inside a real controller action.
        var controllerName = ExtractControllerFromViewPath(viewPath);
        if (controllerName is not null)
        {
            routeData.Values["controller"] = controllerName;
        }

        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());

        var viewResult = viewPath.StartsWith('~') || viewPath.StartsWith('/')
            ? _viewEngine.GetView(executingFilePath: null, viewPath, isMainPage: true)
            : _viewEngine.FindView(actionContext, viewPath, isMainPage: true);

        if (!viewResult.Success)
        {
            var locations = string.Join("\n", viewResult.SearchedLocations ?? Array.Empty<string>());
            throw new InvalidOperationException(
                $"Razor view '{viewPath}' was not found. Searched:\n{locations}");
        }

        await using var writer = new StringWriter();
        var viewDictionary = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = model
        };

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            new TempDataDictionary(httpContext, _tempDataProvider),
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }

    private static string? ExtractControllerFromViewPath(string viewPath)
    {
        var trimmed = viewPath.TrimStart('~', '/');
        var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 3 && string.Equals(segments[0], "Views", StringComparison.OrdinalIgnoreCase))
        {
            return segments[1];
        }
        return null;
    }
}
