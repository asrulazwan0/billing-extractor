using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Infrastructure.Services;

public class FileProcessor : IFileProcessor
{
    private readonly ILogger<FileProcessor> _logger;

    public FileProcessor(ILogger<FileProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]> ReadFileAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    public string CalculateFileHash(byte[] fileData)
    {
        if (fileData == null)
            throw new ArgumentNullException(nameof(fileData));

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileData);
        return Convert.ToBase64String(hashBytes);
    }

    public bool IsValidFileType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var validExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".bmp" };
        
        var isValid = validExtensions.Contains(extension);
        
        _logger.LogDebug("File type validation for {FileName}: {IsValid}", fileName, isValid);
        
        return isValid;
    }
}