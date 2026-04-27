using Microsoft.AspNetCore.Html;

namespace FundingPlatform.Web.Models;

public enum DataTableDensity
{
    Comfortable = 0,
    Compact = 1
}

public sealed record DataTableViewModel(
    string Caption,
    IReadOnlyList<string> ColumnHeaders,
    IReadOnlyList<IReadOnlyList<IHtmlContent>> Rows,
    DataTableDensity Density = DataTableDensity.Comfortable,
    EmptyStateViewModel? EmptyState = null);
