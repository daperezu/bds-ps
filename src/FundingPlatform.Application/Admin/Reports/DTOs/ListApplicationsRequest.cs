using FundingPlatform.Domain.Enums;

namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed class ListApplicationsRequest
{
    public IReadOnlyList<ApplicationState> States { get; set; } = Array.Empty<ApplicationState>();
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public string? Search { get; set; }
    public bool? HasAgreement { get; set; }
    public bool? HasActiveAppeal { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string Sort { get; set; } = "updated-desc";
}
