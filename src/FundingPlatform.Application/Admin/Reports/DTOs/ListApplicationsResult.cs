namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed record ListApplicationsResult(
    IReadOnlyList<ApplicationRowDto> Rows,
    int TotalCount,
    ListApplicationsRequest AppliedFilter);
