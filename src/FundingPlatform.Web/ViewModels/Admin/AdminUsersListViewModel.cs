namespace FundingPlatform.Web.ViewModels.Admin;

public class AdminUsersListViewModel
{
    public IReadOnlyList<AdminUserSummaryRowViewModel> Rows { get; init; } = Array.Empty<AdminUserSummaryRowViewModel>();
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? RoleFilter { get; init; }
    public string? StatusFilter { get; init; }
    public string? Search { get; init; }

    public int TotalPages =>
        PageSize <= 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
