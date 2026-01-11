using MediatR;
using BillingExtractor.Application.Interfaces;
using BillingExtractor.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Application.Commands.Handlers;

public class ProcessInvoicesCommandHandler : IRequestHandler<ProcessInvoicesCommand, ProcessInvoicesResponse>
{
    private readonly IInvoiceExtractor _invoiceExtractor;
    private readonly IInvoiceValidator _invoiceValidator;
    private readonly IFileProcessor _fileProcessor;
    private readonly ILogger<ProcessInvoicesCommandHandler> _logger;

    public ProcessInvoicesCommandHandler(
        IInvoiceExtractor invoiceExtractor,
        IInvoiceValidator invoiceValidator,
        IFileProcessor fileProcessor,
        ILogger<ProcessInvoicesCommandHandler> logger)
    {
        _invoiceExtractor = invoiceExtractor ?? throw new ArgumentNullException(nameof(invoiceExtractor));
        _invoiceValidator = invoiceValidator ?? throw new ArgumentNullException(nameof(invoiceValidator));
        _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProcessInvoicesResponse> Handle(ProcessInvoicesCommand request, CancellationToken cancellationToken)
    {
        var response = new ProcessInvoicesResponse();
        var filesToProcess = new List<(Stream Stream, string FileName)>();

        try
        {
            _logger.LogInformation("Processing {FileCount} invoice files", request.Files.Length);

            // Validate and prepare files
            foreach (var file in request.Files)
            {
                if (!_fileProcessor.IsValidFileType(file.FileName))
                {
                    response.Errors.Add($"Invalid file type: {file.FileName}");
                    response.TotalFailed++;
                    continue;
                }

                var stream = file.OpenReadStream();
                filesToProcess.Add((stream, file.FileName));
            }

            if (!filesToProcess.Any())
            {
                response.Errors.Add("No valid files to process");
                return response;
            }

            // Extract invoices using AI/LLM
            var extractedInvoices = await _invoiceExtractor.ExtractInvoicesAsync(filesToProcess, cancellationToken);

            foreach (var invoice in extractedInvoices)
            {
                try
                {
                    // Apply validations if enabled
                    if (request.EnableValidation)
                    {
                        var validationResult = await _invoiceValidator.ValidateInvoiceAsync(invoice, cancellationToken);

                        invoice.ValidationWarnings.AddRange(validationResult.Warnings);
                        invoice.ValidationErrors.AddRange(validationResult.Errors);

                        if (!validationResult.IsValid)
                        {
                            _logger.LogWarning("Invoice {InvoiceNumber} failed validation", invoice.InvoiceNumber);
                            response.TotalFailed++;
                            response.Invoices.Add(invoice);
                            continue;
                        }
                    }

                    // Check for duplicates if enabled
                    if (request.EnableDuplicateDetection)
                    {
                        var isDuplicate = await _invoiceValidator.IsDuplicateAsync(invoice, cancellationToken);
                        if (isDuplicate)
                        {
                            invoice.ValidationWarnings.Add(new ValidationWarningDto
                            {
                                Code = "DUPLICATE",
                                Message = "Possible duplicate invoice detected"
                            });
                            response.TotalDuplicates++;
                        }
                    }

                    invoice.Status = "Processed";
                    response.Invoices.Add(invoice);
                    response.TotalProcessed++;

                    _logger.LogInformation("Successfully processed invoice {InvoiceNumber}", invoice.InvoiceNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing invoice");
                    response.Errors.Add($"Error processing {invoice.InvoiceNumber}: {ex.Message}");
                    response.TotalFailed++;

                    invoice.Status = "Failed";
                    invoice.ProcessingError = ex.Message;
                    response.Invoices.Add(invoice);
                }
            }

            response.Success = response.TotalProcessed > 0;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoices batch");
            response.Errors.Add($"Batch processing failed: {ex.Message}");
            response.Success = false;
            return response;
        }
        finally
        {
            // Ensure all streams are disposed
            foreach (var (stream, _) in filesToProcess)
            {
                try
                {
                    await stream.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing stream");
                }
            }
        }
    }
}