namespace FundingPlatform.Application.Admin.Users.DTOs;

public record ListUsersRequest(
    string? RoleFilter,
    string? StatusFilter,
    string? Search,
    int Page,
    int PageSize);
