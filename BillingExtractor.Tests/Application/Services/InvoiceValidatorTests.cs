using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Interfaces;
using BillingExtractor.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BillingExtractor.Tests.Application.Services;

public class InvoiceValidatorTests
{
    private readonly Mock<ILogger<InvoiceValidator>> _mockLogger;
    private readonly InvoiceValidator _validator;

    public InvoiceValidatorTests()
    {
        _mockLogger = new Mock<ILogger<InvoiceValidator>>();
        _validator = new InvoiceValidator(_mockLogger.Object);
    }

    [Fact]
    public async Task ValidateInvoiceAsync_ValidInvoice_ReturnsValidResult()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ValidateInvoiceAsync_EmptyInvoiceNumber_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == nameof(invoiceDto.InvoiceNumber));
    }

    [Fact]
    public async Task ValidateInvoiceAsync_NullInvoiceNumber_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = null!,
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == nameof(invoiceDto.InvoiceNumber));
    }

    [Fact]
    public async Task ValidateInvoiceAsync_ZeroTotalAmount_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 0,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == nameof(invoiceDto.TotalAmount));
    }

    [Fact]
    public async Task ValidateInvoiceAsync_NegativeTotalAmount_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = -100.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == nameof(invoiceDto.TotalAmount));
    }

    [Fact]
    public async Task ValidateInvoiceAsync_EmptyVendorName_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == nameof(invoiceDto.VendorName));
    }

    [Fact]
    public async Task ValidateInvoiceAsync_EmptyCustomerName_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == nameof(invoiceDto.CustomerName));
    }

    [Fact]
    public async Task ValidateInvoiceAsync_NoLineItems_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>()
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == nameof(invoiceDto.LineItems));
    }

    [Fact]
    public async Task ValidateInvoiceAsync_LineItemWithEmptyDescription_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "", // Empty description
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "LineItem[0].Description");
    }

    [Fact]
    public async Task ValidateInvoiceAsync_LineItemWithZeroQuantity_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 0, // Zero quantity
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "LineItem[0].Quantity");
    }

    [Fact]
    public async Task ValidateInvoiceAsync_LineItemWithNegativeQuantity_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = -1, // Negative quantity
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "LineItem[0].Quantity");
    }

    [Fact]
    public async Task ValidateInvoiceAsync_LineItemWithNegativeUnitPrice_ReturnsError()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = -500.00m, // Negative unit price
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "LineItem[0].UnitPrice");
    }

    [Fact]
    public async Task ValidateInvoiceAsync_MultipleLineItemsWithSameDescription_AddsWarning()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Same Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 250.00m,
                    LineTotal = 500.00m
                },
                new LineItemDto
                {
                    LineNumber = 2,
                    Description = "Same Item", // Same description as first item
                    Quantity = 1,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 500.00m
                }
            }
        };

        // Act
        var result = await _validator.ValidateInvoiceAsync(invoiceDto);

        // Assert
        Assert.True(result.IsValid); // Still valid but with warnings
        Assert.Contains(result.Warnings, w => w.Code == "LineItems");
    }

    [Fact]
    public async Task IsDuplicateAsync_AlwaysReturnsFalse()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 1000.00m,
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 500.00m,
                    LineTotal = 1000.00m
                }
            }
        };

        // Act
        var result = await _validator.IsDuplicateAsync(invoiceDto);

        // Assert
        Assert.False(result);
    }
}