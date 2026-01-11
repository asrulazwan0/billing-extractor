using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Infrastructure.Services;

public class InvoiceValidator : IInvoiceValidator
{
    private readonly ILogger<InvoiceValidator> _logger;

    public InvoiceValidator(
        ILogger<InvoiceValidator> logger)
    {
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

        result.Errors = errors;
        result.Warnings = warnings;
        result.IsValid = !errors.Any();

        _logger.LogInformation("Invoice validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
            result.IsValid, result.Errors.Count, result.Warnings.Count);

        return await Task.FromResult(result);
    }

    public async Task<bool> IsDuplicateAsync(InvoiceDto invoice, CancellationToken cancellationToken = default)
    {
        // This is a simplified implementation - in a real scenario, you'd check against the database
        // For now, we'll return false to allow all invoices
        _logger.LogInformation("Checking for duplicate invoice: {InvoiceNumber}", invoice.InvoiceNumber);
        
        // In a real implementation, you would query the database to check if an invoice with the same
        // number, provider, and date already exists
        return await Task.FromResult(false);
    }
}