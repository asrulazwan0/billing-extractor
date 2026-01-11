namespace BillingExtractor.Domain.Entities;

public class ValidationWarning
{
    public string Code { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    // Private constructor for EF Core
    private ValidationWarning() { }

    public static ValidationWarning Create(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required", nameof(message));

        return new ValidationWarning
        {
            Code = code.Trim(),
            Message = message.Trim()
        };
    }
}