namespace FundingPlatform.Web.ViewModels.Admin;

public class AdminUserSummaryRowViewModel
{
    public string Id { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsSelf { get; init; }
}
