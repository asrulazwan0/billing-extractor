using BillingExtractor.Domain.Entities;

namespace BillingExtractor.Application.DTOs;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? Subtotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public List<LineItemDto> LineItems { get; set; } = new();
    public List<ValidationWarningDto> ValidationWarnings { get; set; } = new();
    public List<ValidationErrorDto> ValidationErrors { get; set; } = new();
    public double? ConfidenceScore { get; set; }
    public string? ProcessingError { get; set; }
}

public class LineItemDto
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class ValidationWarningDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ValidationErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ProcessInvoicesResponse
{
    public bool Success { get; set; }
    public List<InvoiceDto> Invoices { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalProcessed { get; set; }
    public int TotalFailed { get; set; }
    public int TotalDuplicates { get; set; }
}

public class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}