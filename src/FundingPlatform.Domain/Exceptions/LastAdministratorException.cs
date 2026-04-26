namespace FundingPlatform.Domain.Exceptions;

public sealed class LastAdministratorException : Exception
{
    public string ErrorCode { get; } = "LAST_ADMIN_PROTECTED";

    public LastAdministratorException()
        : base("Cannot disable the last remaining administrator. Promote another user to Admin first.")
    {
    }

    public LastAdministratorException(string message)
        : base(message)
    {
    }
}
