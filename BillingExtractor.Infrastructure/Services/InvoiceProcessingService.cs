using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Interfaces;
using BillingExtractor.Domain.Entities;
using BillingExtractor.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Infrastructure.Services;

public class InvoiceProcessingService : IInvoiceProcessingService
{
    private readonly IInvoiceExtractor _extractor;
    private readonly IInvoiceRepository _repository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<InvoiceProcessingService> _logger;

    public InvoiceProcessingService(
        IInvoiceExtractor extractor,
        IInvoiceRepository repository,
        IFileStorageService fileStorageService,
        ILogger<InvoiceProcessingService> logger)
    {
        _extractor = extractor;
        _repository = repository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<Guid> ProcessInvoiceAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting invoice processing for {FileName}", fileName);

        // Calculate file hash for duplicate detection
        var fileHash = await _fileStorageService.CalculateFileHashAsync(fileStream, cancellationToken);

        // Check if invoice with same hash already exists
        var existingInvoice = await _repository.GetByFileHashAsync(fileHash, cancellationToken);
        if (existingInvoice != null)
        {
            _logger.LogWarning("Duplicate invoice detected with hash {FileHash}", fileHash);
            throw new InvalidOperationException($"Invoice with the same content already exists: {existingInvoice.InvoiceNumber}");
        }

        // Save file to storage
        var filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, cancellationToken);

        try
        {
            // Extract invoice data using LLM
            var extractedInvoice = await _extractor.ExtractInvoiceAsync(fileStream, fileName, cancellationToken);

            // Validate extracted data
            var validationResults = ValidateInvoice(extractedInvoice);
            
            // Create domain entity
            var invoice = new Invoice
            {
                Id = extractedInvoice.Id != Guid.Empty ? extractedInvoice.Id : Guid.NewGuid(),
                InvoiceNumber = extractedInvoice.InvoiceNumber,
                InvoiceDate = extractedInvoice.InvoiceDate,
                DueDate = extractedInvoice.DueDate,
                VendorName = extractedInvoice.VendorName,
                CustomerName = extractedInvoice.CustomerName,
                TotalAmount = new Money(extractedInvoice.TotalAmount, extractedInvoice.Currency),
                TaxAmount = extractedInvoice.TaxAmount.HasValue ? new Money(extractedInvoice.TaxAmount.Value, extractedInvoice.Currency) : null,
                Subtotal = extractedInvoice.Subtotal.HasValue ? new Money(extractedInvoice.Subtotal.Value, extractedInvoice.Currency) : null,
                OriginalFileName = fileName,
                FilePath = filePath,
                FileHash = fileHash,
                Status = InvoiceStatus.Processed,
                ProcessedAt = DateTime.UtcNow,
                ValidationErrors = validationResults.Errors.Select(e => new ValidationError(e.Code, e.Message)).ToList(),
                ValidationWarnings = validationResults.Warnings.Select(w => new ValidationWarning(w.Code, w.Message)).ToList(),
                LineItems = extractedInvoice.LineItems?.Select(li => new LineItem
                {
                    Id = li.Id != Guid.Empty ? li.Id : Guid.NewGuid(),
                    Description = li.Description,
                    Quantity = li.Quantity,
                    Unit = li.Unit,
                    UnitPrice = new Money(li.UnitPrice, extractedInvoice.Currency),
                    LineTotal = new Money(li.LineTotal, extractedInvoice.Currency),
                    LineNumber = li.LineNumber
                }).ToList() ?? new List<LineItem>()
            };

            // Check for similar invoices
            var similarInvoices = await _repository.FindSimilarAsync(
                invoice.InvoiceNumber, 
                invoice.VendorName, 
                invoice.InvoiceDate, 
                cancellationToken);

            if (similarInvoices.Any())
            {
                invoice.ValidationWarnings.Add(new ValidationWarning("DUPLICATE_POSSIBLE", 
                    $"Similar invoice(s) with number {invoice.InvoiceNumber} already exist for vendor {invoice.VendorName}"));
            }

            // Save to database
            await _repository.AddAsync(invoice, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice {InvoiceNumber} processed successfully", invoice.InvoiceNumber);

            return invoice.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice {FileName}", fileName);

            // Create failed invoice record
            var failedInvoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "PROCESSING_FAILED",
                InvoiceDate = DateTime.UtcNow,
                VendorName = "Unknown",
                CustomerName = "Unknown",
                TotalAmount = new Money(0, "USD"),
                OriginalFileName = fileName,
                FilePath = filePath,
                FileHash = fileHash,
                Status = InvoiceStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                ProcessingError = ex.Message
            };

            await _repository.AddAsync(failedInvoice, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            throw;
        }
    }

    public async Task<List<Guid>> ProcessInvoicesAsync(List<(Stream Stream, string FileName)> files, CancellationToken cancellationToken = default)
    {
        var results = new List<Guid>();

        foreach (var file in files)
        {
            try
            {
                var invoiceId = await ProcessInvoiceAsync(file.Stream, file.FileName, cancellationToken);
                results.Add(invoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process invoice {FileName}", file.FileName);
                // Continue with other files
            }
        }

        return results;
    }

    private (List<ValidationErrorDto> Errors, List<ValidationWarningDto> Warnings) ValidateInvoice(InvoiceDto invoice)
    {
        var errors = new List<ValidationErrorDto>();
        var warnings = new List<ValidationWarningDto>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            errors.Add(new ValidationErrorDto { Code = "MISSING_INVOICE_NUMBER", Message = "Invoice number is required" });
        }

        if (string.IsNullOrWhiteSpace(invoice.VendorName))
        {
            errors.Add(new ValidationErrorDto { Code = "MISSING_VENDOR_NAME", Message = "Vendor name is required" });
        }

        if (invoice.TotalAmount <= 0)
        {
            errors.Add(new ValidationErrorDto { Code = "INVALID_TOTAL_AMOUNT", Message = "Total amount must be greater than zero" });
        }

        // Validate line items
        if (invoice.LineItems?.Any() != true)
        {
            warnings.Add(new ValidationWarningDto { Code = "NO_LINE_ITEMS", Message = "Invoice has no line items" });
        }
        else
        {
            for (int i = 0; i < invoice.LineItems.Count; i++)
            {
                var item = invoice.LineItems[i];
                
                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    errors.Add(new ValidationErrorDto { Code = "MISSING_DESCRIPTION", Message = $"Line item {i + 1} is missing description" });
                }
                
                if (item.Quantity <= 0)
                {
                    errors.Add(new ValidationErrorDto { Code = "INVALID_QUANTITY", Message = $"Line item {i + 1} has invalid quantity" });
                }
                
                if (item.UnitPrice < 0)
                {
                    errors.Add(new ValidationErrorDto { Code = "NEGATIVE_PRICE", Message = $"Line item {i + 1} has negative unit price" });
                }
            }
        }

        // Validate amounts consistency
        if (invoice.LineItems?.Any() == true)
        {
            var calculatedTotal = invoice.LineItems.Sum(li => li.LineTotal);
            if (Math.Abs(calculatedTotal - invoice.TotalAmount) > 0.01m) // Allow small rounding differences
            {
                warnings.Add(new ValidationWarningDto { Code = "AMOUNT_MISMATCH", Message = "Calculated total doesn't match invoice total" });
            }
        }

        return (errors, warnings);
    }
}