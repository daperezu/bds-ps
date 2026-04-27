namespace FundingPlatform.Application.Admin.Users.DTOs;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string InitialPassword,
    string? LegalId);
