namespace FundingPlatform.Application.Admin.Users.DTOs;

public record DomainError(string Code, string? Field, string Message);
