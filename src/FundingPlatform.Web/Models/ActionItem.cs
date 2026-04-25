namespace FundingPlatform.Web.Models;

public sealed record ActionItem(
    string Label,
    ActionClass Class,
    string? Url = null,
    string? FormController = null,
    string? FormAction = null,
    object? FormRouteValues = null,
    string? Icon = null,
    string? ConfirmDialogId = null);
