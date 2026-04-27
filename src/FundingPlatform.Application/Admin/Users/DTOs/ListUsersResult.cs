namespace FundingPlatform.Application.Admin.Users.DTOs;

public record ListUsersResult(
    IReadOnlyList<UserSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
