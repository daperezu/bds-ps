namespace FundingPlatform.Domain.Exceptions;

public enum SelfModificationAction
{
    DisableSelf,
    ChangeOwnRole,
    ChangeOwnEmail,
    ResetOwnPassword,
}

public sealed class SelfModificationException : Exception
{
    public string ErrorCode { get; } = "SELF_MODIFICATION_BLOCKED";

    public SelfModificationAction Action { get; }

    public SelfModificationException(SelfModificationAction action)
        : base(MessageFor(action))
    {
        Action = action;
    }

    public SelfModificationException(SelfModificationAction action, string message)
        : base(message)
    {
        Action = action;
    }

    private static string MessageFor(SelfModificationAction action) => action switch
    {
        SelfModificationAction.DisableSelf => "Administrators cannot disable their own account.",
        SelfModificationAction.ChangeOwnRole => "Administrators cannot change their own role from the admin area.",
        SelfModificationAction.ChangeOwnEmail => "Administrators cannot change their own email from the admin area.",
        SelfModificationAction.ResetOwnPassword => "Administrators cannot reset their own password from the admin area.",
        _ => "Administrators cannot perform this action on their own account from the admin area.",
    };
}
