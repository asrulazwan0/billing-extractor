using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Infrastructure.Services;

public class InvoiceValidator : IInvoiceValidator
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<InvoiceValidator> _logger;

    public InvoiceValidator(
        IInvoiceRepository invoiceRepository,
        ILogger<InvoiceValidator> logger)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationResult> ValidateInvoiceAsync(InvoiceDto invoice, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();
        var errors = new List<ValidationErrorDto>();
        var warnings = new List<ValidationWarningDto>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            errors.Add(new ValidationErrorDto { Code = nameof(invoice.InvoiceNumber), Message = "Invoice number is required" });

        if (invoice.InvoiceDate == DateTime.MinValue)
            errors.Add(new ValidationErrorDto { Code = nameof(invoice.InvoiceDate), Message = "Invoice date is required" });

        if (invoice.TotalAmount <= 0)
            errors.Add(new ValidationErrorDto { Code = nameof(invoice.TotalAmount), Message = "Total amount must be greater than zero" });

        if (string.IsNullOrWhiteSpace(invoice.VendorName))
            errors.Add(new ValidationErrorDto { Code = nameof(invoice.VendorName), Message = "Vendor name is required" });

        if (string.IsNullOrWhiteSpace(invoice.CustomerName))
            errors.Add(new ValidationErrorDto { Code = nameof(invoice.CustomerName), Message = "Customer name is required" });

        // Validate line items
        if (invoice.LineItems?.Any() != true)
        {
            errors.Add(new ValidationErrorDto { Code = nameof(invoice.LineItems), Message = "At least one line item is required" });
        }
        else
        {
            for (int i = 0; i < invoice.LineItems.Count; i++)
            {
                var item = invoice.LineItems[i];
                if (string.IsNullOrWhiteSpace(item.Description))
                    errors.Add(new ValidationErrorDto { Code = $"LineItem[{i}].Description", Message = "Line item description is required" });

                if (item.Quantity <= 0)
                    errors.Add(new ValidationErrorDto { Code = $"LineItem[{i}].Quantity", Message = "Line item quantity must be greater than zero" });

                if (item.UnitPrice <= 0)
                    errors.Add(new ValidationErrorDto { Code = $"LineItem[{i}].UnitPrice", Message = "Line item unit price must be greater than zero" });
            }
        }

        // Check for duplicate line items
        var groupedItems = invoice.LineItems?.GroupBy(x => x.Description).Where(g => g.Count() > 1).ToList();
        if (groupedItems?.Any() == true)
        {
            foreach (var group in groupedItems)
            {
                warnings.Add(new ValidationWarningDto { Code = "LineItems", Message = $"Multiple line items with description '{group.Key}'" });
            }
        }

        // Amount Verification: Validate that line item amounts sum up correctly to the stated total
        if (invoice.LineItems?.Any() == true)
        {
            var calculatedTotal = invoice.LineItems.Sum(item => item.LineTotal);
            var statedTotal = invoice.TotalAmount;

            // Using a small epsilon for decimal comparison if needed, but here simple != should work for currency
            if (Math.Abs(calculatedTotal - statedTotal) > 0.01m)
            {
                warnings.Add(new ValidationWarningDto 
                { 
                    Code = "AMOUNT_MISMATCH", 
                    Message = $"Sum of line items ({calculatedTotal}) does not match invoice total ({statedTotal})." 
                });
            }
        }

        result.Errors = errors;
        result.Warnings = warnings;
        result.IsValid = !errors.Any();

        _logger.LogInformation("Invoice validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
            result.IsValid, result.Errors.Count, result.Warnings.Count);

        return await Task.FromResult(result);
    }

    public async Task<bool> IsDuplicateAsync(InvoiceDto invoice, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking for duplicate invoice: {InvoiceNumber} for vendor {VendorName}", 
            invoice.InvoiceNumber, invoice.VendorName);
        
        return await _invoiceRepository.ExistsAsync(
            invoice.InvoiceNumber, 
            invoice.VendorName, 
            invoice.InvoiceDate, 
            cancellationToken);
    }
}