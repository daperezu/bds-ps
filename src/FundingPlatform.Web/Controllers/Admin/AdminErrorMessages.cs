namespace FundingPlatform.Web.Controllers.Admin;

internal static class AdminErrorMessages
{
    public const string SentinelImmutable =
        "This account is a system account and cannot be modified.";

    public const string LastAdminProtected =
        "Cannot disable the last remaining administrator. Promote another user to Admin first.";

    public const string SelfDisable =
        "Administrators cannot disable their own account.";

    public const string SelfChangeRole =
        "Administrators cannot change their own role from the admin area.";

    public const string SelfChangeEmail =
        "Administrators cannot change their own email from the admin area.";

    public const string SelfResetPassword =
        "Administrators cannot reset their own password from the admin area.";
}
