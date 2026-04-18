namespace FundingPlatform.Application.DTOs;

public record FundingAgreementPanelDto(
    int ApplicationId,
    bool AgreementExists,
    bool CanGenerate,
    bool CanRegenerate,
    string? DisabledReason,
    DateTime? GeneratedAtUtc,
    string? GeneratedByUserId,
    string? GeneratedByDisplayName);
