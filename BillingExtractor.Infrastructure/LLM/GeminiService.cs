using Google.Cloud.AIPlatform.V1;
using Google.Protobuf;
using System.Text;
using BillingExtractor.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingExtractor.Infrastructure.LLM;

public class GeminiService : BaseLLMService
{
    private readonly GeminiOptions _options;
    private readonly PredictionServiceClient _predictionServiceClient;

    public GeminiService(
        IOptions<GeminiOptions> options,
        ILogger<GeminiService> logger) : base(logger)
    {
        _options = options.Value;
        // Note: In production, you'd want to properly initialize the client
        // This is simplified for the assessment
        _predictionServiceClient = PredictionServiceClient.Create();
    }

    public override async Task<InvoiceDto> ExtractInvoiceAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extracting invoice from {FileName} using Gemini", fileName);

            // Read file content
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            // Convert to base64 for Gemini
            var base64Content = Convert.ToBase64String(fileBytes);

            // Determine MIME type
            var mimeType = GetMimeType(fileName);

            // Create prompt with image
            var prompt = $"{CreateExtractionPrompt()}\n\nFile: {fileName}\nMIME Type: {mimeType}";

            // Call Gemini API
            var request = new GenerateContentRequest
            {
                Model = $"projects/{_options.ProjectId}/locations/{_options.Location}/publishers/google/models/{_options.ModelId}",
                Contents =
                {
                    new Content
                    {
                        Role = "user",
                        Parts =
                        {
                            new Part { Text = prompt },
                            new Part
                            {
                                InlineData = new Blob
                                {
                                    MimeType = mimeType,
                                    Data = ByteString.CopyFrom(fileBytes)
                                }
                            }
                        }
                    }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.1f,
                    TopP = 0.8f,
                    TopK = 40,
                    MaxOutputTokens = 2048,
                    ResponseMimeType = "application/json"
                }
            };

            var response = await _predictionServiceClient.GenerateContentAsync(request, cancellationToken);

            if (response.Candidates.Count == 0)
            {
                throw new InvalidOperationException("No response from Gemini API");
            }

            var jsonResponse = response.Candidates[0].Content.Parts[0].Text;

            _logger.LogDebug("Gemini response received: {Response}", jsonResponse);

            return ParseLLMResponse(jsonResponse, fileName);
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
}

public class GeminiOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string Location { get; set; } = "us-central1";
    public string ModelId { get; set; } = "gemini-1.5-pro";
    public string? ApiKey { get; set; }
}