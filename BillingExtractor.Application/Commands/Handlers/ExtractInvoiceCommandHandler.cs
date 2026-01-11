using MediatR;
using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Application.Commands.Handlers;

public class ExtractInvoiceCommandHandler : IRequestHandler<ExtractInvoiceCommand, DTOs.InvoiceDto>
{
    private readonly IInvoiceExtractor _invoiceExtractor;
    private readonly ILogger<ExtractInvoiceCommandHandler> _logger;

    public ExtractInvoiceCommandHandler(
        IInvoiceExtractor invoiceExtractor,
        ILogger<ExtractInvoiceCommandHandler> logger)
    {
        _invoiceExtractor = invoiceExtractor ?? throw new ArgumentNullException(nameof(invoiceExtractor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DTOs.InvoiceDto> Handle(ExtractInvoiceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extracting invoice from file: {FileName}", request.FileName);

        await using var stream = new MemoryStream(request.FileContent);
        var invoice = await _invoiceExtractor.ExtractInvoiceAsync(stream, request.FileName, cancellationToken);

        _logger.LogInformation("Successfully extracted invoice {InvoiceNumber}", invoice.InvoiceNumber);
        return invoice;
    }
}