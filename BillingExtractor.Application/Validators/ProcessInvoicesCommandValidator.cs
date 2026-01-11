using FluentValidation;
using BillingExtractor.Application.Commands;

namespace BillingExtractor.Application.Validators;

public class ProcessInvoicesCommandValidator : AbstractValidator<ProcessInvoicesCommand>
{
    public ProcessInvoicesCommandValidator()
    {
        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("At least one file is required")
            .Must(files => files.Length <= 10).WithMessage("Maximum 10 files allowed per request");

        RuleForEach(x => x.Files)
            .Must(file => file.Length > 0).WithMessage("File cannot be empty")
            .Must(file => file.Length <= 10 * 1024 * 1024).WithMessage("File size cannot exceed 10MB")
            .Must((command, file) =>
                file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                file.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                file.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                file.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only PDF, JPG, JPEG, and PNG files are allowed");
    }
}