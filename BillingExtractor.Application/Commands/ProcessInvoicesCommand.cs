using MediatR;
using Microsoft.AspNetCore.Http;
using BillingExtractor.Application.DTOs;

namespace BillingExtractor.Application.Commands;

public class ProcessInvoicesCommand : IRequest<ProcessInvoicesResponse>
{
    public IFormFile[] Files { get; }
    public bool EnableValidation { get; }
    public bool EnableDuplicateDetection { get; }

    public ProcessInvoicesCommand(IFormFile[] files, bool enableValidation = true, bool enableDuplicateDetection = true)
    {
        Files = files ?? throw new ArgumentNullException(nameof(files));
        EnableValidation = enableValidation;
        EnableDuplicateDetection = enableDuplicateDetection;
    }
}