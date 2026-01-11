using System.ComponentModel.DataAnnotations;
using BillingExtractor.Domain.Common;
using BillingExtractor.Domain.ValueObjects;

namespace BillingExtractor.Domain.Entities;

public class Invoice : EntityBase
{
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateTime InvoiceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public Money TotalAmount { get; private set; } = null!;
    public Money? TaxAmount { get; private set; }
    public Money? Subtotal { get; private set; }
    public string? OriginalFileName { get; private set; }
    public string? FilePath { get; private set; }
    public string? FileHash { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Pending;
    public DateTime ProcessedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<LineItem> LineItems { get; private set; } = new List<LineItem>();
    public ICollection<ValidationWarning> ValidationWarnings { get; private set; } = new List<ValidationWarning>();
    public ICollection<ValidationError> ValidationErrors { get; private set; } = new List<ValidationError>();
    public string? ProcessingError { get; private set; }

    public void SetProcessingError(string? error)
    {
        ProcessingError = error;
    }

    // Private constructor for EF Core
    private Invoice() { }

    public static Invoice Create(
        string invoiceNumber,
        DateTime invoiceDate,
        string vendorName,
        string customerName,
        Money totalAmount,
        Money? taxAmount = null,
        Money? subtotal = null,
        DateTime? dueDate = null,
        string currency = "USD",
        string? originalFileName = null,
        string? filePath = null,
        string? fileHash = null)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required", nameof(invoiceNumber));

        if (string.IsNullOrWhiteSpace(vendorName))
            throw new ArgumentException("Vendor name is required", nameof(vendorName));

        if (totalAmount.Amount < 0)
            throw new ArgumentException("Total amount cannot be negative", nameof(totalAmount));

        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber.Trim(),
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            VendorName = vendorName.Trim(),
            CustomerName = customerName.Trim(),
            Currency = currency,
            TotalAmount = totalAmount,
            TaxAmount = taxAmount,
            Subtotal = subtotal,
            OriginalFileName = originalFileName,
            FilePath = filePath,
            FileHash = fileHash,
            Status = InvoiceStatus.Pending,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public void UpdateStatus(InvoiceStatus status)
    {
        Status = status;
        ProcessedAt = DateTime.UtcNow;
    }

    public void AddValidationError(string code, string message)
    {
        ValidationErrors.Add(ValidationError.Create(code, message));
    }

    public void AddValidationWarning(string code, string message)
    {
        ValidationWarnings.Add(ValidationWarning.Create(code, message));
    }

    public void SetFileMetadata(string? originalFileName, string? filePath, string? fileHash)
    {
        OriginalFileName = originalFileName;
        FilePath = filePath;
        FileHash = fileHash;
    }

    public void AddLineItem(LineItem lineItem)
    {
        LineItems.Add(lineItem);
    }
}

public enum InvoiceStatus
{
    Pending,
    Processed,
    Failed,
    Duplicate
}