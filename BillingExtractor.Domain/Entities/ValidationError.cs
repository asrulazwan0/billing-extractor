namespace BillingExtractor.Domain.Entities;

public class ValidationError
{
    public string Code { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

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