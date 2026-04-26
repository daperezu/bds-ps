namespace FundingPlatform.Application.Admin.Users.DTOs;

public record UserDetailDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string Status,
    string? LegalId,
    bool MustChangePassword);
