namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed record ListAgingApplicationsResult(
    IReadOnlyList<AgingApplicationRowDto> Rows,
    int TotalCount,
    ListAgingApplicationsRequest AppliedFilter);
