namespace FundingPlatform.Domain.ValueObjects;

public sealed record CurrencyAmount
{
    public string Currency { get; }
    public decimal Amount { get; }

    public CurrencyAmount(string currency, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        var canonical = currency.Trim().ToUpperInvariant();
        if (canonical.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-character code.", nameof(currency));
        }

        Currency = canonical;
        Amount = amount;
    }
}
