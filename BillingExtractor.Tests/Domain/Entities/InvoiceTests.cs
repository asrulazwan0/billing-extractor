using BillingExtractor.Domain.Entities;
using BillingExtractor.Domain.ValueObjects;
using Xunit;

namespace BillingExtractor.Tests.Domain.Entities;

public class InvoiceTests
{
    [Fact]
    public void Create_ValidParameters_CreatesInvoice()
    {
        // Arrange
        var invoiceNumber = "INV-001";
        var invoiceDate = DateTime.Now;
        var vendorName = "Test Vendor";
        var customerName = "Test Customer";
        var totalAmount = new Money(1000.00m, "USD");

        // Act
        var invoice = Invoice.Create(invoiceNumber, invoiceDate, vendorName, customerName, totalAmount);

        // Assert
        Assert.Equal(invoiceNumber, invoice.InvoiceNumber);
        Assert.Equal(invoiceDate.Date, invoice.InvoiceDate.Date);
        Assert.Equal(vendorName, invoice.VendorName);
        Assert.Equal(customerName, invoice.CustomerName);
        Assert.Equal(totalAmount, invoice.TotalAmount);
        Assert.Equal(InvoiceStatus.Pending, invoice.Status);
    }

    [Fact]
    public void Create_EmptyInvoiceNumber_ThrowsArgumentException()
    {
        // Arrange
        var invoiceNumber = "";
        var invoiceDate = DateTime.Now;
        var vendorName = "Test Vendor";
        var customerName = "Test Customer";
        var totalAmount = new Money(1000.00m, "USD");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Invoice.Create(invoiceNumber, invoiceDate, vendorName, customerName, totalAmount));
        Assert.Contains("Invoice number is required", ex.Message);
    }

    [Fact]
    public void Create_NullInvoiceNumber_ThrowsArgumentException()
    {
        // Arrange
        string invoiceNumber = null!;
        var invoiceDate = DateTime.Now;
        var vendorName = "Test Vendor";
        var customerName = "Test Customer";
        var totalAmount = new Money(1000.00m, "USD");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Invoice.Create(invoiceNumber, invoiceDate, vendorName, customerName, totalAmount));
        Assert.Contains("Invoice number is required", ex.Message);
    }

    [Fact]
    public void Create_EmptyVendorName_ThrowsArgumentException()
    {
        // Arrange
        var invoiceNumber = "INV-001";
        var invoiceDate = DateTime.Now;
        var vendorName = "";
        var customerName = "Test Customer";
        var totalAmount = new Money(1000.00m, "USD");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            Invoice.Create(invoiceNumber, invoiceDate, vendorName, customerName, totalAmount));
        Assert.Contains("Vendor name is required", ex.Message);
    }

    [Fact]
    public void Create_NegativeTotalAmount_ThrowsArgumentException()
    {
        // This test is actually testing the Money constructor validation, not the Invoice.Create validation
        // The Money constructor itself throws when amount is negative
        var ex = Assert.Throws<ArgumentException>(() => new Money(-100.00m, "USD"));
        Assert.Contains("Amount cannot be negative", ex.Message);
    }

    [Fact]
    public void UpdateStatus_UpdatesStatusAndTimestamp()
    {
        // Arrange
        var invoice = Invoice.Create("INV-001", DateTime.Now, "Test Vendor", "Test Customer", new Money(1000.00m, "USD"));
        var originalProcessedAt = invoice.ProcessedAt;
        
        // Wait a moment to ensure timestamp difference
        System.Threading.Thread.Sleep(1);

        // Act
        invoice.UpdateStatus(InvoiceStatus.Processed);

        // Assert
        Assert.Equal(InvoiceStatus.Processed, invoice.Status);
        Assert.NotEqual(originalProcessedAt, invoice.ProcessedAt);
    }

    [Fact]
    public void AddValidationError_AddsErrorToList()
    {
        // Arrange
        var invoice = Invoice.Create("INV-001", DateTime.Now, "Test Vendor", "Test Customer", new Money(1000.00m, "USD"));
        var errorCode = "TEST_ERROR";
        var errorMessage = "Test error message";

        // Act
        invoice.AddValidationError(errorCode, errorMessage);

        // Assert
        Assert.Single(invoice.ValidationErrors);
        Assert.Equal(errorCode, invoice.ValidationErrors.First().Code);
        Assert.Equal(errorMessage, invoice.ValidationErrors.First().Message);
    }

    [Fact]
    public void AddValidationWarning_AddsWarningToList()
    {
        // Arrange
        var invoice = Invoice.Create("INV-001", DateTime.Now, "Test Vendor", "Test Customer", new Money(1000.00m, "USD"));
        var warningCode = "TEST_WARNING";
        var warningMessage = "Test warning message";

        // Act
        invoice.AddValidationWarning(warningCode, warningMessage);

        // Assert
        Assert.Single(invoice.ValidationWarnings);
        Assert.Equal(warningCode, invoice.ValidationWarnings.First().Code);
        Assert.Equal(warningMessage, invoice.ValidationWarnings.First().Message);
    }

    [Fact]
    public void SetFileMetadata_UpdatesFileProperties()
    {
        // Arrange
        var invoice = Invoice.Create("INV-001", DateTime.Now, "Test Vendor", "Test Customer", new Money(1000.00m, "USD"));
        var fileName = "test_invoice.pdf";
        var filePath = "/uploads/test_invoice.pdf";
        var fileHash = "abc123def456";

        // Act
        invoice.SetFileMetadata(fileName, filePath, fileHash);

        // Assert
        Assert.Equal(fileName, invoice.OriginalFileName);
        Assert.Equal(filePath, invoice.FilePath);
        Assert.Equal(fileHash, invoice.FileHash);
    }

    [Fact]
    public void AddLineItem_AddsItemToList()
    {
        // Arrange
        var invoice = Invoice.Create("INV-001", DateTime.Now, "Test Vendor", "Test Customer", new Money(1000.00m, "USD"));
        var lineItem = LineItem.Create(1, "Test Item", 2, "EA", new Money(50.00m, "USD"), invoice.Id);

        // Act
        invoice.AddLineItem(lineItem);

        // Assert
        Assert.Single(invoice.LineItems);
        Assert.Equal(lineItem, invoice.LineItems.First());
    }

    [Fact]
    public void SetProcessingError_UpdatesErrorProperty()
    {
        // Arrange
        var invoice = Invoice.Create("INV-001", DateTime.Now, "Test Vendor", "Test Customer", new Money(1000.00m, "USD"));
        var errorMessage = "Processing failed due to invalid format";

        // Act
        invoice.SetProcessingError(errorMessage);

        // Assert
        Assert.Equal(errorMessage, invoice.ProcessingError);
    }
}