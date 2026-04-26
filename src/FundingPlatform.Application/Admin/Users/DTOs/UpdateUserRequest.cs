namespace FundingPlatform.Application.Admin.Users.DTOs;

public record UpdateUserRequest(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string? LegalId);
