namespace FundingPlatform.Web.Models;

public sealed record SidebarEntry(
    string Label,
    string Url,
    string Icon,
    string[] AllowedRoles,
    bool ShowToUnauthenticated = false);
