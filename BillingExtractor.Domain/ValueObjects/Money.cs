namespace BillingExtractor.Domain.ValueObjects;

public class Money
{
    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public Money(decimal amount, string currencyCode)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code is required", nameof(currencyCode));

        Amount = amount;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }

    public static Money operator +(Money left, Money right)
    {
        if (left.CurrencyCode != right.CurrencyCode)
            throw new InvalidOperationException("Cannot add money amounts with different currencies");

        return new Money(left.Amount + right.Amount, left.CurrencyCode);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.CurrencyCode != right.CurrencyCode)
            throw new InvalidOperationException("Cannot subtract money amounts with different currencies");

        var resultAmount = left.Amount - right.Amount;
        if (resultAmount < 0)
            throw new InvalidOperationException("Result cannot be negative");

        return new Money(resultAmount, left.CurrencyCode);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Money other)
        {
            return Amount == other.Amount && CurrencyCode == other.CurrencyCode;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, CurrencyCode);
    }

    public override string ToString()
    {
        return $"{Amount} {CurrencyCode}";
    }

    public static bool operator ==(Money? left, Money? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Money? left, Money? right)
    {
        return !(left == right);
    }
}