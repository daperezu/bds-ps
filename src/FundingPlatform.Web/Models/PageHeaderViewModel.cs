namespace FundingPlatform.Web.Models;

public sealed record PageHeaderViewModel(
    string Title,
    string? Subtitle = null,
    IReadOnlyList<BreadcrumbItem>? Breadcrumbs = null,
    IReadOnlyList<ActionItem>? PrimaryActions = null);
