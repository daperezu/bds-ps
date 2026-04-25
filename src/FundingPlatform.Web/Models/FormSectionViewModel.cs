using Microsoft.AspNetCore.Html;

namespace FundingPlatform.Web.Models;

public sealed record FormSectionViewModel(
    string Label,
    string? Hint = null,
    string ForFieldName = "",
    Func<dynamic, IHtmlContent>? BodyRenderer = null);
