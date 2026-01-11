using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingExtractor.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly LocalFileStorageOptions _options;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IOptions<LocalFileStorageOptions> options, ILogger<LocalFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Ensure the upload directory exists
        if (!Directory.Exists(_options.UploadPath))
        {
            Directory.CreateDirectory(_options.UploadPath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var fileExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_options.UploadPath, uniqueFileName);

        using var file = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(file, cancellationToken);

        _logger.LogInformation("File saved to {FilePath}", filePath);

        return filePath;
    }

    public async Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var memoryStream = new MemoryStream();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("File deleted: {FilePath}", filePath);
            return true;
        }

        return false;
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return File.Exists(filePath);
    }

    public async Task<string> CalculateFileHashAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        fileStream.Position = 0; // Reset position to beginning
        var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
        fileStream.Position = 0; // Reset position again for further use

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

public class LocalFileStorageOptions
{
    public string UploadPath { get; set; } = "uploads";
}