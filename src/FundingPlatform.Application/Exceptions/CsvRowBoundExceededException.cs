namespace FundingPlatform.Application.Exceptions;

public sealed class CsvRowBoundExceededException : Exception
{
    public int Limit { get; }
    public int ActualCount { get; }

    public CsvRowBoundExceededException(int limit, int actualCount)
        : base($"CSV export refused: {actualCount} rows exceeds the configured limit of {limit}. Narrow your filter and try again.")
    {
        Limit = limit;
        ActualCount = actualCount;
    }
}
