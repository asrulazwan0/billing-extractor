using BillingExtractor.Domain.Common;

namespace BillingExtractor.Domain.Entities;

public class ValidationError : EntityBase
{
    public string Code { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public Guid InvoiceId { get; private set; }
    public Invoice Invoice { get; private set; } = null!;

    // Private constructor for EF Core
    private ValidationError() { }

    public static ValidationError Create(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required", nameof(message));

        return new ValidationError
        {
            Code = code.Trim(),
            Message = message.Trim()
        };
    }
}