using BillingExtractor.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace BillingExtractor.Application.Interfaces;

public interface IInvoiceExtractor
{
    Task<InvoiceDto> ExtractInvoiceAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<List<InvoiceDto>> ExtractInvoicesAsync(List<(Stream Stream, string FileName)> files, CancellationToken cancellationToken = default);
}

public interface IInvoiceValidator
{
    Task<ValidationResult> ValidateInvoiceAsync(InvoiceDto invoice, CancellationToken cancellationToken = default);
    Task<bool> IsDuplicateAsync(InvoiceDto invoice, CancellationToken cancellationToken = default);
}

public interface IFileProcessor
{
    Task<byte[]> ReadFileAsync(Stream stream, CancellationToken cancellationToken = default);
    string CalculateFileHash(byte[] fileData);
    bool IsValidFileType(string fileName);
}

public interface IInvoiceProcessingService
{
    Task<ProcessInvoicesResponse> ProcessInvoicesAsync(
        List<IFormFile> files,
        bool validate = true,
        bool checkDuplicates = true,
        CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<string> CalculateFileHashAsync(Stream fileStream, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationWarningDto> Warnings { get; set; } = new();
    public List<ValidationErrorDto> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult WithWarnings(List<ValidationWarningDto> warnings) => new() { IsValid = true, Warnings = warnings };
    public static ValidationResult WithErrors(List<ValidationErrorDto> errors) => new() { IsValid = false, Errors = errors };
}