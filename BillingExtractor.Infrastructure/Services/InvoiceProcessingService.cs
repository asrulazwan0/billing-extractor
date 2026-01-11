using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Interfaces;
using BillingExtractor.Domain.Entities;
using BillingExtractor.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
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
            var invoice = Invoice.Create(
                extractedInvoice.InvoiceNumber,
                extractedInvoice.InvoiceDate,
                extractedInvoice.VendorName,
                extractedInvoice.CustomerName,
                new Money(extractedInvoice.TotalAmount, extractedInvoice.Currency),
                extractedInvoice.TaxAmount.HasValue ? new Money(extractedInvoice.TaxAmount.Value, extractedInvoice.Currency) : null,
                extractedInvoice.Subtotal.HasValue ? new Money(extractedInvoice.Subtotal.Value, extractedInvoice.Currency) : null,
                extractedInvoice.DueDate,
                extractedInvoice.Currency
            );

            invoice.SetFileMetadata(fileName, filePath, fileHash);
            invoice.UpdateStatus(InvoiceStatus.Processed);

            // Add validation results
            foreach (var error in validationResults.Errors)
            {
                invoice.AddValidationError(error.Code, error.Message);
            }

            foreach (var warning in validationResults.Warnings)
            {
                invoice.AddValidationWarning(warning.Code, warning.Message);
            }

            // Add line items
            if (extractedInvoice.LineItems != null)
            {
                foreach (var li in extractedInvoice.LineItems)
                {
                    var moneyUnitPrice = new Money(li.UnitPrice, extractedInvoice.Currency);
                    var lineItem = LineItem.Create(
                        li.LineNumber,
                        li.Description,
                        li.Quantity,
                        li.Unit,
                        moneyUnitPrice,
                        invoice.Id
                    );

                    invoice.AddLineItem(lineItem);
                }
            }

            // Check for similar invoices
            var similarInvoices = await _repository.FindSimilarAsync(
                invoice.InvoiceNumber, 
                invoice.VendorName, 
                invoice.InvoiceDate, 
                cancellationToken);

            if (similarInvoices.Any())
            {
                invoice.AddValidationWarning("DUPLICATE_POSSIBLE",
                    $"Similar invoice(s) with number {invoice.InvoiceNumber} already exist for vendor {invoice.VendorName}");
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
            var failedInvoice = Invoice.Create(
                "PROCESSING_FAILED",
                DateTime.UtcNow,
                "Unknown",
                "Unknown",
                new Money(0, "USD")
            );

            failedInvoice.SetFileMetadata(fileName, filePath, fileHash);
            failedInvoice.UpdateStatus(InvoiceStatus.Failed);
            failedInvoice.SetProcessingError(ex.Message);

            await _repository.AddAsync(failedInvoice, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            throw;
        }
    }

    public async Task<ProcessInvoicesResponse> ProcessInvoicesAsync(List<IFormFile> files, bool validate = true, bool checkDuplicates = true, CancellationToken cancellationToken = default)
    {
        var response = new ProcessInvoicesResponse();
        var results = new List<InvoiceDto>();

        foreach (var file in files)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var invoice = await _extractor.ExtractInvoiceAsync(stream, file.FileName, cancellationToken);

                // Add validation logic if enabled
                if (validate)
                {
                    // Perform validation here
                }

                // Check for duplicates if enabled
                if (checkDuplicates)
                {
                    // Check for duplicates here
                }

                results.Add(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process invoice {FileName}", file.FileName);
                response.Errors.Add($"Failed to process {file.FileName}: {ex.Message}");
            }
        }

        response.Success = !response.Errors.Any();
        response.Invoices = results;
        response.TotalProcessed = results.Count;
        response.TotalFailed = response.Errors.Count;

        return response;
    }

    public async Task<List<Guid>> ProcessInvoicesAsListAsync(List<(Stream Stream, string FileName)> files, CancellationToken cancellationToken = default)
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