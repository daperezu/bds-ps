namespace FundingPlatform.Application.DTOs;

public record GenerateAgreementQueueRowDto(
    int ApplicationId,
    string ApplicantDisplayName,
    DateTime ResponseFinalizedAtUtc);
