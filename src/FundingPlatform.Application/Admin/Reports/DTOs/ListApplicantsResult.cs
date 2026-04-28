namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed record ListApplicantsResult(
    IReadOnlyList<ApplicantRowDto> Rows,
    int TotalCount,
    ListApplicantsRequest AppliedFilter);
