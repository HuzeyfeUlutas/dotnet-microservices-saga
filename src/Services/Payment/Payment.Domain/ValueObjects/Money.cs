using Payment.Domain.Exceptions;

namespace Payment.Domain.ValueObjects;

public sealed class Money
{
    private Money()
    {
    }

    public Money(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new DomainException("Amount must be greater than zero.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = NormalizeCurrency(currency);
    }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;

    public void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException("Money currency mismatch.");
        }
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency cannot be empty.");
        }

        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new DomainException("Currency must be a three-letter ISO code.");
        }

        return normalized;
    }
}
