namespace BillingExtractor.Domain.ValueObjects;

public record Money : IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));
        
        Amount = amount;
        Currency = currency.ToUpper();
    }
    
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {left.Currency} and {right.Currency}");
        
        return new Money(left.Amount + right.Amount, left.Currency);
    }
    
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot subtract different currencies: {left.Currency} and {right.Currency}");
        
        return new Money(left.Amount - right.Amount, left.Currency);
    }
    
    public static bool operator <(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Amount < right.Amount;
    }
    
    public static bool operator >(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Amount > right.Amount;
    }
    
    private static void ValidateSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare different currencies: {left.Currency} and {right.Currency}");
    }
    
    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        ValidateSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }
    
    public Money WithAmount(decimal newAmount) => new(newAmount, Currency);
    
    public override string ToString() => $"{Currency} {Amount:F2}";
    
    public string ToFormattedString() => 
        string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:C}", Amount).Replace("$", Currency + " ");
}