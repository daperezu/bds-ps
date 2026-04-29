namespace FundingPlatform.Application.Errors;

/// <summary>
/// Spec 012 / FR-014 — Application-layer user-facing failure reasons.
///
/// Application services raise these codes (instead of inline English sentinel
/// strings) at the Application/Web boundary. The Web layer maps each code to
/// a Spanish (es-CR) string via <c>IUserFacingErrorTranslator</c> before the
/// message reaches the user via TempData / ModelState.
///
/// NFR-001 invariant: all Application-layer code, logs, and exception
/// messages stay English. Only the Web layer translates these codes into
/// Spanish for end-user surfaces.
/// </summary>
public enum UserFacingErrorCode
{
    /// <summary>Catch-all for a domain rule rejection whose original English
    /// message we still want to surface verbatim (e.g. <c>InvalidOperationException</c>
    /// thrown by domain entities). The Web layer renders the generic Spanish
    /// equivalent; the original English detail is logged but not displayed.</summary>
    OperationRejected,

    // Application aggregate
    ApplicationNotFound,
    ApplicationNotUnderReview,
    ApplicationItemNotFound,
    ApplicationNotOwnedByApplicant,
    SupplierRequiredOnApprove,
    InvalidReviewDecision,
    ConcurrentApplicationModification,

    // Appeal aggregate
    AppealAccessDenied,
    NoOpenAppealForMessage,
    UnknownAppealResolution,
    ConcurrentAppealModification,

    // Funding-agreement aggregate
    AgreementGenerationPreconditionsNotMet,
    AgreementRegenerationPreconditionsNotMet,
    AgreementPdfRenderingFailed,
    AgreementGenerationFailed,
    ConcurrentAgreementModification,

    // Signed upload (resource not found / authz)
    SignedUploadResourceNotFound,
    ConcurrentSignedUploadModification,

    // Signed upload (validation)
    SignedUploadStaleAgreementVersion,
    SignedUploadAlreadyPending,
    SignedUploadNoPendingToReplace,
    SignedUploadWrongPendingId,
    SignedUploadNoPendingToWithdraw,
    SignedUploadStalePendingId,
    SignedUploadNoPendingToApprove,
    SignedUploadNoPendingToReject,
    SignedUploadRejectionCommentRequired,

    // Signed upload (intake validation)
    SignedUploadUnsupportedContentType,
    SignedUploadFileEmpty,
    SignedUploadFileTooLarge,
    SignedUploadContentUnreadable,
    SignedUploadNotAPdf,
    SignedUploadMissingPdfHeader,
}
