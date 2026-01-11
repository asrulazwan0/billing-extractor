using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Interfaces;
using BillingExtractor.Domain.Entities;
using BillingExtractor.Domain.ValueObjects;
using BillingExtractor.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BillingExtractor.Tests.Infrastructure.Services;

public class InvoiceProcessingServiceValidationTests
{
    [Fact]
    public void ValidateInvoice_AmountMismatch_AddsWarning()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 100.00m, // Actual total should be 90.00 (60+30)
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Item 1",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 30.00m,
                    LineTotal = 60.00m
                },
                new LineItemDto
                {
                    LineNumber = 2,
                    Description = "Item 2",
                    Quantity = 1,
                    Unit = "EA",
                    UnitPrice = 30.00m,
                    LineTotal = 30.00m
                }
            }
        };

        // Create a mock service to access the validation logic
        var mockExtractor = new Mock<IInvoiceExtractor>();
        var mockRepository = new Mock<IInvoiceRepository>();
        var mockFileStorageService = new Mock<IFileStorageService>();
        var mockLogger = new Mock<ILogger<InvoiceProcessingService>>();

        var service = new InvoiceProcessingService(
            mockExtractor.Object,
            mockRepository.Object,
            mockFileStorageService.Object,
            mockLogger.Object);

        // Use reflection to access the private ValidateInvoice method
        var method = typeof(InvoiceProcessingService).GetMethod("ValidateInvoice", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(service, new object[] { invoiceDto });
        var (errors, warnings) = ((List<ValidationErrorDto>, List<ValidationWarningDto>))result;

        // Assert - Check that there are no errors but there is a warning about amount mismatch
        Assert.Empty(errors);
        Assert.Contains(warnings, w => w.Code == "AMOUNT_MISMATCH");
    }

    [Fact]
    public void ValidateInvoice_CorrectAmounts_NoWarnings()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Now,
            VendorName = "Test Vendor",
            CustomerName = "Test Customer",
            TotalAmount = 90.00m, // Matches calculated total: (2*30) + (1*30) = 90
            Currency = "USD",
            LineItems = new List<LineItemDto>
            {
                new LineItemDto
                {
                    LineNumber = 1,
                    Description = "Item 1",
                    Quantity = 2,
                    Unit = "EA",
                    UnitPrice = 30.00m,
                    LineTotal = 60.00m
                },
                new LineItemDto
                {
                    LineNumber = 2,
                    Description = "Item 2",
                    Quantity = 1,
                    Unit = "EA",
                    UnitPrice = 30.00m,
                    LineTotal = 30.00m
                }
            }
        };

        // Create a mock service to access the validation logic
        var mockExtractor = new Mock<IInvoiceExtractor>();
        var mockRepository = new Mock<IInvoiceRepository>();
        var mockFileStorageService = new Mock<IFileStorageService>();
        var mockLogger = new Mock<ILogger<InvoiceProcessingService>>();

        var service = new InvoiceProcessingService(
            mockExtractor.Object,
            mockRepository.Object,
            mockFileStorageService.Object,
            mockLogger.Object);

        // Use reflection to access the private ValidateInvoice method
        var method = typeof(InvoiceProcessingService).GetMethod("ValidateInvoice", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(service, new object[] { invoiceDto });
        var (errors, warnings) = ((List<ValidationErrorDto>, List<ValidationWarningDto>))result;

        // Assert - Check that there are no errors or warnings about amount mismatch
        Assert.Empty(errors);
        Assert.DoesNotContain(warnings, w => w.Code == "AMOUNT_MISMATCH");
    }
}