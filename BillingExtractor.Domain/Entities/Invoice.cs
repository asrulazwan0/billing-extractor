using BillingExtractor.Domain.Common;
using BillingExtractor.Domain.ValueObjects;

namespace BillingExtractor.Domain.Entities;

public class Invoice : EntityBase, IAggregateRoot
{
    // Invoice identification
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateTime InvoiceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    
    // Parties
    public string VendorName { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    
    // Monetary values
    public Money TotalAmount { get; private set; } = new(0, "USD");
    public Money? TaxAmount { get; private set; }
    public Money? Subtotal { get; private set; }
    public string Currency => TotalAmount.Currency;
    
    // File information
    public string OriginalFileName { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    
    // Processing status
    public ProcessingStatus Status { get; private set; } = ProcessingStatus.Pending;
    public DateTime ProcessedAt { get; private set; }
    public string? ProcessingError { get; private set; }
    
    // Line items
    private readonly List<LineItem> _lineItems = new();
    public IReadOnlyCollection<LineItem> LineItems => _lineItems.AsReadOnly();
    
    // Validations
    private readonly List<ValidationWarning> _validationWarnings = new();
    public IReadOnlyCollection<ValidationWarning> ValidationWarnings => _validationWarnings.AsReadOnly();
    
    private readonly List<ValidationError> _validationErrors = new();
    public IReadOnlyCollection<ValidationError> ValidationErrors => _validationErrors.AsReadOnly();
    
    // Private constructor for EF Core
    private Invoice() { }
    
    // Factory method
    public static Invoice Create(
        string invoiceNumber,
        DateTime invoiceDate,
        string vendorName,
        string customerName,
        Money totalAmount,
        string originalFileName,
        string fileHash)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required", nameof(invoiceNumber));
        
        if (string.IsNullOrWhiteSpace(vendorName))
            throw new ArgumentException("Vendor name is required", nameof(vendorName));
        
        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber.Trim(),
            InvoiceDate = invoiceDate,
            VendorName = vendorName.Trim(),
            CustomerName = (customerName ?? string.Empty).Trim(),
            TotalAmount = totalAmount,
            OriginalFileName = originalFileName,
            FileHash = fileHash,
            Status = ProcessingStatus.Pending,
            ProcessedAt = DateTime.UtcNow
        };
        
        invoice.AddDomainEvent(new InvoiceCreatedEvent(invoice.Id));
        
        return invoice;
    }
    
    // Domain methods
    public void AddLineItem(LineItem lineItem)
    {
        if (lineItem == null)
            throw new ArgumentNullException(nameof(lineItem));
        
        if (lineItem.LineTotal.Currency != TotalAmount.Currency)
            throw new InvalidOperationException($"Line item currency {lineItem.LineTotal.Currency} must match invoice currency {TotalAmount.Currency}");
        
        _lineItems.Add(lineItem);
    }
    
    public void AddValidationWarning(string code, string message)
    {
        _validationWarnings.Add(new ValidationWarning(code, message));
        AddDomainEvent(new InvoiceValidationWarningEvent(Id, code, message));
    }
    
    public void AddValidationError(string code, string message)
    {
        _validationErrors.Add(new ValidationError(code, message));
        AddDomainEvent(new InvoiceValidationErrorEvent(Id, code, message));
        Status = ProcessingStatus.Failed;
    }
    
    public void MarkAsProcessing()
    {
        Status = ProcessingStatus.Processing;
        AddDomainEvent(new InvoiceProcessingEvent(Id));
    }
    
    public void MarkAsCompleted()
    {
        Status = ProcessingStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        AddDomainEvent(new InvoiceProcessedEvent(Id));
    }
    
    public void MarkAsFailed(string error)
    {
        Status = ProcessingStatus.Failed;
        ProcessingError = error;
        AddDomainEvent(new InvoiceFailedEvent(Id, error));
    }
    
    public void SetDueDate(DateTime dueDate) => DueDate = dueDate;
    public void SetTaxAmount(Money taxAmount) => TaxAmount = taxAmount;
    public void SetSubtotal(Money subtotal) => Subtotal = subtotal;
    public void SetFilePath(string filePath) => FilePath = filePath;
    
    // Business logic
    public bool IsDuplicateOf(Invoice other)
    {
        if (other == null) return false;
        
        return InvoiceNumber == other.InvoiceNumber &&
               VendorName == other.VendorName &&
               Math.Abs(TotalAmount.Amount - other.TotalAmount.Amount) < 0.01m &&
               InvoiceDate.Date == other.InvoiceDate.Date;
    }
    
    public bool ValidateAmounts()
    {
        if (!_lineItems.Any()) return true;
        
        var calculatedTotal = _lineItems.Sum(item => item.LineTotal.Amount);
        var difference = Math.Abs(TotalAmount.Amount - calculatedTotal);
        
        if (difference > 0.01m)
        {
            AddValidationWarning("AMOUNT_MISMATCH", 
                $"Invoice total {TotalAmount.Amount:F2} doesn't match sum of line items {calculatedTotal:F2}. Difference: {difference:F2}");
            return false;
        }
        
        return true;
    }
}

public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public record ValidationWarning(string Code, string Message);
public record ValidationError(string Code, string Message);