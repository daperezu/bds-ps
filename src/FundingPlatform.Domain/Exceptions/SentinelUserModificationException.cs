namespace FundingPlatform.Domain.Exceptions;

public sealed class SentinelUserModificationException : Exception
{
    public string ErrorCode { get; } = "SENTINEL_IMMUTABLE";

    public SentinelUserModificationException()
        : base("This account is a system account and cannot be modified.")
    {
    }

    public SentinelUserModificationException(string message)
        : base(message)
    {
    }
}
