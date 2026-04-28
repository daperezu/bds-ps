namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed record ListFundedItemsResult(
    IReadOnlyList<FundedItemRowDto> Rows,
    int TotalCount,
    ListFundedItemsRequest AppliedFilter);
