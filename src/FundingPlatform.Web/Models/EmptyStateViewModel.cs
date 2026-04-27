namespace FundingPlatform.Web.Models;

public sealed record EmptyStateViewModel(
    string Headline,
    string Body,
    string Icon = "ti ti-mood-empty",
    ActionItem? PrimaryAction = null);
