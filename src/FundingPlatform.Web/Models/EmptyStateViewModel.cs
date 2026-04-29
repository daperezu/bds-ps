namespace FundingPlatform.Web.Models;

/// <summary>
/// Empty-state contract. Spec 011 (FR-064) extends spec 008's icon-only model
/// to require an <see cref="IllustrationSceneKey"/> illustration in most contexts.
/// The icon-only fallback is preserved for the auth-layout AccessDenied case
/// where decorative illustrations are intentionally absent (research §9, FR-018).
/// </summary>
public sealed record EmptyStateViewModel(
    string Headline,
    string Body,
    string Icon = "ti ti-mood-empty",
    ActionItem? PrimaryAction = null,
    string? IllustrationSceneKey = null,
    string? IllustrationAltText = null);
