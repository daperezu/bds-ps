namespace FundingPlatform.Application.DTOs;

public record SigningStagePanelDto(
    int ApplicationId,
    bool AgreementExists,
    bool CanGenerate,
    bool CanRegenerate,
    string? DisabledReason,
    DateTime? GeneratedAtUtc,
    string? GeneratedByUserId,
    string? GeneratedByDisplayName,
    int GeneratedVersion,
    SignedUploadSummaryDto? PendingUpload,
    SigningReviewDecisionDto? LastDecision,
    int? ApprovedSignedUploadId,
    bool CanApplicantUpload,
    bool CanApplicantReplaceOrWithdraw,
    bool CanReviewerAct,
    bool IsExecuted);
