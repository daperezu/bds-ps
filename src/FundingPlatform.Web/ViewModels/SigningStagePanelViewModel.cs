using FundingPlatform.Application.DTOs;

namespace FundingPlatform.Web.ViewModels;

public class SigningStagePanelViewModel
{
    public int ApplicationId { get; set; }
    public bool AgreementExists { get; set; }
    public string? AgreementDownloadUrl { get; set; }
    public bool CanGenerate { get; set; }
    public bool CanRegenerate { get; set; }
    public string? DisabledReason { get; set; }
    public DateTime? GeneratedAtUtc { get; set; }
    public string? GeneratedByDisplayName { get; set; }
    public int GeneratedVersion { get; set; }
    public bool ShowActions { get; set; }

    public SignedUploadSummaryDto? PendingUpload { get; set; }
    public SigningReviewDecisionDto? LastDecision { get; set; }
    public int? ApprovedSignedUploadId { get; set; }
    public string? ApprovedSignedDownloadUrl { get; set; }

    public bool CanApplicantUpload { get; set; }
    public bool CanApplicantReplaceOrWithdraw { get; set; }
    public bool CanReviewerAct { get; set; }
    public bool IsExecuted { get; set; }

    public string? UploadSuccessMessage { get; set; }
    public string? UploadErrorMessage { get; set; }
    public string? VersionMismatchMessage { get; set; }
    public string? RejectCommentError { get; set; }
}
