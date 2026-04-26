using FundingPlatform.Domain.Enums;
using FundingPlatform.Web.Models;

namespace FundingPlatform.Web.Helpers;

public static class StatusVisualMap
{
    public static StatusVisual For(ApplicationState s) => s switch
    {
        ApplicationState.Draft => new StatusVisual("bg-secondary", "ti ti-pencil", "Draft"),
        ApplicationState.Submitted => new StatusVisual("bg-info", "ti ti-send", "Submitted"),
        ApplicationState.UnderReview => new StatusVisual("bg-primary", "ti ti-eye", "Under Review"),
        ApplicationState.Resolved => new StatusVisual("bg-success", "ti ti-circle-check", "Resolved"),
        ApplicationState.AppealOpen => new StatusVisual("bg-warning", "ti ti-alert-triangle", "Appeal Open"),
        ApplicationState.ResponseFinalized => new StatusVisual("bg-info", "ti ti-checks", "Response Finalized"),
        ApplicationState.AgreementExecuted => new StatusVisual("bg-success", "ti ti-file-check", "Agreement Executed"),
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unhandled ApplicationState"),
    };

    public static StatusVisual For(ItemReviewStatus s) => s switch
    {
        ItemReviewStatus.Pending => new StatusVisual("bg-secondary", "ti ti-clock", "Pending"),
        ItemReviewStatus.Approved => new StatusVisual("bg-success", "ti ti-check", "Approved"),
        ItemReviewStatus.Rejected => new StatusVisual("bg-danger", "ti ti-x", "Rejected"),
        ItemReviewStatus.NeedsInfo => new StatusVisual("bg-warning", "ti ti-help-circle", "Needs Info"),
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unhandled ItemReviewStatus"),
    };

    public static StatusVisual For(AppealStatus s) => s switch
    {
        AppealStatus.Open => new StatusVisual("bg-warning", "ti ti-alert-triangle", "Open"),
        AppealStatus.Resolved => new StatusVisual("bg-success", "ti ti-circle-check", "Resolved"),
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unhandled AppealStatus"),
    };

    public static StatusVisual For(SignedUploadStatus s) => s switch
    {
        SignedUploadStatus.Pending => new StatusVisual("bg-secondary", "ti ti-clock", "Pending"),
        SignedUploadStatus.Approved => new StatusVisual("bg-success", "ti ti-check", "Approved"),
        SignedUploadStatus.Rejected => new StatusVisual("bg-danger", "ti ti-x", "Rejected"),
        SignedUploadStatus.Superseded => new StatusVisual("bg-secondary", "ti ti-arrow-back-up", "Superseded"),
        SignedUploadStatus.Withdrawn => new StatusVisual("bg-secondary", "ti ti-arrow-back-up", "Withdrawn"),
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unhandled SignedUploadStatus"),
    };

    public static StatusVisual For(AdminUserStatus s) => s switch
    {
        AdminUserStatus.Active => new StatusVisual("bg-success", "ti ti-circle-check", "Active"),
        AdminUserStatus.Disabled => new StatusVisual("bg-secondary", "ti ti-ban", "Disabled"),
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unhandled AdminUserStatus"),
    };

    public static StatusVisual For(AdminUserRole r) => r switch
    {
        AdminUserRole.Applicant => new StatusVisual("bg-info", "ti ti-user", "Applicant"),
        AdminUserRole.Reviewer => new StatusVisual("bg-primary", "ti ti-eye", "Reviewer"),
        AdminUserRole.Admin => new StatusVisual("bg-purple", "ti ti-shield-lock", "Admin"),
        _ => throw new ArgumentOutOfRangeException(nameof(r), r, "Unhandled AdminUserRole"),
    };
}
