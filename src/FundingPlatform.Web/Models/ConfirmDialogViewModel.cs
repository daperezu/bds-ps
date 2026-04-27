namespace FundingPlatform.Web.Models;

public sealed record ConfirmDialogViewModel(
    string Id,
    string Title,
    string IrreversibilityRationale,
    string ConfirmLabel,
    string CancelLabel = "Cancel",
    ActionClass ConfirmClass = ActionClass.Destructive,
    string FormController = "",
    string FormAction = "",
    object? FormRouteValues = null);
