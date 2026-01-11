using MediatR;
using BillingExtractor.Application.DTOs;

namespace BillingExtractor.Application.Commands;

public class ExtractInvoiceCommand : IRequest<InvoiceDto>
{
    public byte[] FileContent { get; }
    public string FileName { get; }
    public bool Validate { get; }

    public ExtractInvoiceCommand(byte[] fileContent, string fileName, bool validate = true)
    {
        FileContent = fileContent ?? throw new ArgumentNullException(nameof(fileContent));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Validate = validate;
    }
}