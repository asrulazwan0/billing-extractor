using System.Net.Http.Headers;
using System.Text;
using BillingExtractor.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingExtractor.Infrastructure.LLM;

public class OpenAIService : BaseLLMService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;

    public OpenAIService(
        HttpClient httpClient,
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIService> logger) : base(logger)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public override async Task<InvoiceDto> ExtractInvoiceAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extracting invoice from {FileName} using OpenAI", fileName);

            // Read file content
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();
            var base64Content = Convert.ToBase64String(fileBytes);

            // Determine MIME type
            var mimeType = GetMimeType(fileName);

            // Create request
            var request = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = CreateExtractionPrompt() },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:{mimeType};base64,{base64Content}"
                                }
                            }
                        }
                    }
                },
                max_tokens = 2000,
                temperature = 0.1,
                response_format = new { type = "json_object" }
            };

            var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_options.Endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"OpenAI API request failed: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = System.Text.Json.JsonDocument.Parse(responseContent);

            var jsonResponse = responseObject.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            _logger.LogDebug("OpenAI response received: {Response}", jsonResponse);

            return ParseLLMResponse(jsonResponse!, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API error for {FileName}", fileName);
            return CreateFailedInvoiceDto(fileName, $"OpenAI API error: {ex.Message}");
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

public class OpenAIOptions
{
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4-vision-preview";
}