namespace FundingPlatform.Application.DTOs;

public record SystemConfigurationDto(
    int Id,
    string Key,
    string Value,
    string? Description);
