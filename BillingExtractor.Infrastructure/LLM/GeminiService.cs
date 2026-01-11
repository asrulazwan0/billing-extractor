using System.Net.Http.Headers;
using System.Text;
using BillingExtractor.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingExtractor.Infrastructure.LLM;

public class GeminiService : BaseLLMService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiService> logger) : base(logger)
    {
        _httpClient = httpClient;
        _options = options.Value;

        // Set up the base address for Gemini API
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    }

    public override async Task<InvoiceDto> ExtractInvoiceAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extracting invoice from {FileName} using Gemini", fileName);

            // Copy the stream to a memory stream to prevent disposal issues
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                fileBytes = memoryStream.ToArray();
            }

            var base64Content = Convert.ToBase64String(fileBytes);
            var mimeType = GetMimeType(fileName);

            // Create the request payload
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = CreateExtractionPrompt() },
                            new { inline_data = new { mime_type = mimeType, data = base64Content } }
                        }
                    }
                }
            };

            var jsonRequest = System.Text.Json.JsonSerializer.Serialize(requestBody, new System.Text.Json.JsonSerializerOptions());
            
            using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var apiUrl = $"v1/models/{_options.ModelId}:generateContent?key={_options.ApiKey}";

            using var response = await _httpClient.PostAsync(apiUrl, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, responseContent);
                throw new HttpRequestException($"Gemini API request failed: {response.StatusCode} - {responseContent}");
            }

            _logger.LogDebug("Gemini API response received: {Response}", responseContent);

            // Check if response is an error message instead of expected format
            if (responseContent.Contains("\"error\":", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Gemini API returned error: {Response}", responseContent);
                throw new HttpRequestException($"Gemini API returned error: {responseContent}");
            }

            return ParseGeminiResponse(responseContent, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API error for {FileName}", fileName);
            return CreateFailedInvoiceDto(fileName, $"Gemini API error: {ex.Message}");
        }
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }

    private InvoiceDto ParseGeminiResponse(string jsonResponse, string fileName)
    {
        try
        {
            // Parse the response to extract the text content from Gemini
            using var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                throw new Exception("No candidates in Gemini response");
            }

            var contentElement = candidates[0].GetProperty("content");
            var parts = contentElement.GetProperty("parts");
            if (parts.GetArrayLength() == 0)
            {
                throw new Exception("No parts in Gemini content");
            }

            var textElement = parts[0].GetProperty("text");
            var content = textElement.GetString();

            if (string.IsNullOrEmpty(content))
                throw new Exception("Empty response from Gemini");

            // Sometimes Gemini wraps JSON in markdown blocks
            if (content.StartsWith("```json", StringComparison.Ordinal))
            {
                int startIndex = 7; // Length of "```json"
                int endIndex = content.LastIndexOf("```");
                if (endIndex > startIndex)
                {
                    content = content.Substring(startIndex, endIndex - startIndex);
                }
                else
                {
                    // If there's no closing ``` , take everything after ```json
                    content = content.Substring(startIndex);
                }
                content = content.Trim();
            }

            // Now parse the actual JSON data
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var extractedData = System.Text.Json.JsonSerializer.Deserialize<ExtractedInvoice>(content, options);

            if (extractedData == null)
                throw new Exception("Failed to deserialize extraction results");

            // Map the extracted data to InvoiceDto
            var invoiceDto = new InvoiceDto
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = extractedData.InvoiceNumber ?? $"UNKNOWN_{Guid.NewGuid().ToString()[..8]}",
                InvoiceDate = extractedData.InvoiceDate ?? DateTime.UtcNow,
                DueDate = extractedData.DueDate,
                VendorName = extractedData.VendorName ?? "Unknown Vendor",
                CustomerName = extractedData.CustomerName ?? string.Empty,
                Currency = extractedData.Currency?.ToUpper() ?? "USD",
                TotalAmount = extractedData.TotalAmount ?? 0,
                TaxAmount = extractedData.TaxAmount,
                Subtotal = extractedData.SubTotal,
                Status = "Extracted",
                ProcessedAt = DateTime.UtcNow,
                LineItems = extractedData.LineItems?.Select((item, index) => new LineItemDto
                {
                    LineNumber = index + 1,
                    Description = item.Description ?? $"Item {index + 1}",
                    Quantity = item.Quantity ?? 0,
                    Unit = item.Unit ?? string.Empty,
                    UnitPrice = item.UnitPrice ?? 0,
                    LineTotal = item.LineTotal ?? (item.Quantity * item.UnitPrice) ?? 0
                }).ToList() ?? new List<LineItemDto>(),
                ValidationErrors = new List<ValidationErrorDto>() // Initialize as empty
            };

            return invoiceDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response: {Response}", jsonResponse);
            return CreateFailedInvoiceDto(fileName, $"Failed to parse Gemini response: {ex.Message}");
        }
    }

    private class ExtractedInvoice
    {
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? VendorName { get; set; }
        public string? CustomerName { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Currency { get; set; }
        public List<ExtractedLineItem>? LineItems { get; set; }
    }

    private class ExtractedLineItem
    {
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? LineTotal { get; set; }
    }
}

public class GeminiOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string Location { get; set; } = "us-central1";
    public string ModelId { get; set; } = "gemini-1.5-flash";
    public string? ApiKey { get; set; }
}