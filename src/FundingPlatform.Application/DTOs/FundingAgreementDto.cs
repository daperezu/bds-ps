namespace FundingPlatform.Application.DTOs;

public record FundingAgreementDto(
    int ApplicationId,
    string FileName,
    string ContentType,
    long Size,
    DateTime GeneratedAtUtc,
    string GeneratedByUserId);
