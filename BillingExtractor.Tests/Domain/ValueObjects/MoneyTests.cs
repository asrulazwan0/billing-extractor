using BillingExtractor.Domain.ValueObjects;
using Xunit;

namespace BillingExtractor.Tests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        decimal amount = 100.50m;
        string currencyCode = "USD";

        // Act
        var money = new Money(amount, currencyCode);

        // Assert
        Assert.Equal(amount, money.Amount);
        Assert.Equal("USD", money.CurrencyCode);
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        decimal amount = -100.50m;
        string currencyCode = "USD";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Money(amount, currencyCode));
        Assert.Contains("Amount cannot be negative", ex.Message);
    }

    [Fact]
    public void Constructor_EmptyCurrencyCode_ThrowsArgumentException()
    {
        // Arrange
        decimal amount = 100.50m;
        string currencyCode = "";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Money(amount, currencyCode));
        Assert.Contains("Currency code is required", ex.Message);
    }

    [Fact]
    public void Constructor_NullCurrencyCode_ThrowsArgumentException()
    {
        // Arrange
        decimal amount = 100.50m;
        string currencyCode = null!;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Money(amount, currencyCode));
        Assert.Contains("Currency code is required", ex.Message);
    }

    [Fact]
    public void Constructor_WhitespaceCurrencyCode_ThrowsArgumentException()
    {
        // Arrange
        decimal amount = 100.50m;
        string currencyCode = "   ";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Money(amount, currencyCode));
        Assert.Contains("Currency code is required", ex.Message);
    }

    [Fact]
    public void Addition_SameCurrency_AddsAmounts()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(50.25m, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        Assert.Equal(150.75m, result.Amount);
        Assert.Equal("USD", result.CurrencyCode);
    }

    [Fact]
    public void Addition_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(50.25m, "EUR");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => money1 + money2);
        Assert.Equal("Cannot add money amounts with different currencies", ex.Message);
    }

    [Fact]
    public void Subtraction_SameCurrency_SubtractsAmounts()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(25.25m, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        Assert.Equal(75.25m, result.Amount);
        Assert.Equal("USD", result.CurrencyCode);
    }

    [Fact]
    public void Subtraction_ResultNegative_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(25.25m, "USD");
        var money2 = new Money(100.50m, "USD");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => money1 - money2);
        Assert.Equal("Result cannot be negative", ex.Message);
    }

    [Fact]
    public void Subtraction_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(50.25m, "EUR");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => money1 - money2);
        Assert.Equal("Cannot subtract money amounts with different currencies", ex.Message);
    }

    [Fact]
    public void Equality_SameValues_ReturnsTrue()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "USD");

        // Act & Assert
        Assert.True(money1.Equals(money2));
        Assert.True(money1 == money2);
    }

    [Fact]
    public void Equality_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(200.50m, "USD");

        // Act & Assert
        Assert.False(money1.Equals(money2));
        Assert.True(money1 != money2);
    }

    [Fact]
    public void Equality_DifferentCurrencies_ReturnsFalse()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "EUR");

        // Act & Assert
        Assert.False(money1.Equals(money2));
        Assert.True(money1 != money2);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        var money = new Money(100.50m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        Assert.Equal("100.50 USD", result);
    }
}