using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed class ListAgingApplicationsRequest
{
    public IReadOnlyList<ApplicationState> States { get; set; } = Array.Empty<ApplicationState>();
    public int ThresholdDays { get; set; } = 14;
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string Sort { get; set; } = "days-desc";
}
