using System.Text.Json;
using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Infrastructure.LLM;

public abstract class BaseLLMService : IInvoiceExtractor
{
    protected readonly ILogger<BaseLLMService> _logger;

    protected BaseLLMService(ILogger<BaseLLMService> logger)
    {
        _logger = logger;
    }

    public abstract Task<InvoiceDto> ExtractInvoiceAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    public async Task<List<InvoiceDto>> ExtractInvoicesAsync(List<(Stream Stream, string FileName)> files, CancellationToken cancellationToken = default)
    {
        var tasks = files.Select(async file =>
        {
            try
            {
                return await ExtractInvoiceAsync(file.Stream, file.FileName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting invoice from {FileName}", file.FileName);
                return CreateFailedInvoiceDto(file.FileName, ex.Message);
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    protected virtual InvoiceDto CreateFailedInvoiceDto(string fileName, string error)
    {
        return new InvoiceDto
        {
            InvoiceNumber = "EXTRACTION_FAILED",
            VendorName = "Unknown",
            CustomerName = "Unknown",
            Currency = "USD",
            TotalAmount = 0,
            Status = "Failed",
            ProcessedAt = DateTime.UtcNow,
            ProcessingError = error,
            ValidationErrors = new List<ValidationErrorDto>
            {
                new() { Code = "EXTRACTION_ERROR", Message = $"Failed to extract invoice from {fileName}: {error}" }
            }
        };
    }

    protected virtual string CreateExtractionPrompt()
    {
        return @"You are an expert invoice processing system. Extract all information from the provided invoice document and return it in JSON format.

REQUIRED OUTPUT FORMAT (JSON):
{
  ""invoiceNumber"": ""string (required)"",
  ""invoiceDate"": ""string (ISO 8601 date)"",
  ""dueDate"": ""string (ISO 8601 date, optional)"",
  ""vendorName"": ""string (required)"",
  ""customerName"": ""string (optional)"",
  ""currency"": ""string (3-letter code, default: USD)"",
  ""totalAmount"": ""number (required)"",
  ""taxAmount"": ""number (optional)"",
  ""subtotal"": ""number (optional)"",
  ""lineItems"": [
    {
      ""description"": ""string (required)"",
      ""quantity"": ""number (required)"",
      ""unit"": ""string (optional)"",
      ""unitPrice"": ""number (required)"",
      ""lineTotal"": ""number (required)""
    }
  ]
}

RULES:
1. Extract all amounts as numbers (not strings).
2. If currency is not specified, use USD.
3. Format dates as YYYY-MM-DD.
4. If any field cannot be found, use null for optional fields and reasonable defaults for required fields.
5. Ensure lineItems is an array of objects, each object representing one line item.
6. Validate that line total matches quantity * unit price.
7. Return ONLY valid JSON. No preamble, no markdown blocks, no extra text.
8. Ensure all property names are double-quoted.

INVOICE DATA TO EXTRACT:";
    }

    protected virtual InvoiceDto ParseLLMResponse(string jsonResponse, string fileName)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var extractionResult = JsonSerializer.Deserialize<ExtractionResult>(jsonResponse, options);

            if (extractionResult == null)
            {
                throw new InvalidOperationException("Failed to parse LLM response");
            }

            return new InvoiceDto
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = extractionResult.InvoiceNumber ?? $"UNKNOWN_{Guid.NewGuid().ToString()[..8]}",
                InvoiceDate = ParseDate(extractionResult.InvoiceDate) ?? DateTime.UtcNow,
                DueDate = ParseDate(extractionResult.DueDate),
                VendorName = extractionResult.VendorName ?? "Unknown Vendor",
                CustomerName = extractionResult.CustomerName ?? string.Empty,
                Currency = extractionResult.Currency?.ToUpper() ?? "USD",
                TotalAmount = extractionResult.TotalAmount ?? 0,
                TaxAmount = extractionResult.TaxAmount,
                Subtotal = extractionResult.Subtotal,
                Status = "Extracted",
                ProcessedAt = DateTime.UtcNow,
                LineItems = extractionResult.LineItems?.Select((item, index) => new LineItemDto
                {
                    LineNumber = index + 1,
                    Description = item.Description ?? $"Item {index + 1}",
                    Quantity = item.Quantity ?? 1,
                    Unit = item.Unit ?? string.Empty,
                    UnitPrice = item.UnitPrice ?? 0,
                    LineTotal = item.LineTotal ?? item.Quantity * item.UnitPrice ?? 0
                }).ToList() ?? new List<LineItemDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing LLM response");
            return CreateFailedInvoiceDto(fileName, $"Failed to parse LLM response: {ex.Message}");
        }
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return null;
    }

    private class ExtractionResult
    {
        public string? InvoiceNumber { get; set; }
        public string? InvoiceDate { get; set; }
        public string? DueDate { get; set; }
        public string? VendorName { get; set; }
        public string? CustomerName { get; set; }
        public string? Currency { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? Subtotal { get; set; }
        public List<LineItemResult>? LineItems { get; set; }
    }

    private class LineItemResult
    {
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? LineTotal { get; set; }
    }
}