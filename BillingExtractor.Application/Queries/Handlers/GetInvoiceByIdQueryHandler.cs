using MediatR;
using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Application.Queries.Handlers;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, DTOs.InvoiceDto?>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<GetInvoiceByIdQueryHandler> _logger;

    public GetInvoiceByIdQueryHandler(
        IInvoiceRepository invoiceRepository,
        ILogger<GetInvoiceByIdQueryHandler> logger)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DTOs.InvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting invoice by ID: {InvoiceId}", request.Id);

        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken);

        if (invoice == null)
        {
            _logger.LogWarning("Invoice not found with ID: {InvoiceId}", request.Id);
            return null;
        }

        return MapToDto(invoice);
    }

    private DTOs.InvoiceDto MapToDto(Domain.Entities.Invoice invoice)
    {
        return new DTOs.InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            VendorName = invoice.VendorName,
            CustomerName = invoice.CustomerName,
            Currency = invoice.Currency,
            TotalAmount = invoice.TotalAmount.Amount,
            TaxAmount = invoice.TaxAmount?.Amount,
            Subtotal = invoice.Subtotal?.Amount,
            Status = invoice.Status.ToString(),
            ProcessedAt = invoice.ProcessedAt,
            LineItems = invoice.LineItems.Select(li => new DTOs.LineItemDto
            {
                LineNumber = li.LineNumber,
                Description = li.Description,
                Quantity = li.Quantity,
                Unit = li.Unit,
                UnitPrice = li.UnitPrice.Amount,
                LineTotal = li.LineTotal.Amount
            }).ToList(),
            ValidationWarnings = invoice.ValidationWarnings.Select(w => new DTOs.ValidationWarningDto
            {
                Code = w.Code,
                Message = w.Message
            }).ToList(),
            ValidationErrors = invoice.ValidationErrors.Select(e => new DTOs.ValidationErrorDto
            {
                Code = e.Code,
                Message = e.Message
            }).ToList(),
            ProcessingError = invoice.ProcessingError
        };
    }
}