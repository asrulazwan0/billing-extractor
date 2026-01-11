using MediatR;
using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Application.Queries.Handlers;

public class GetAllInvoicesQueryHandler : IRequestHandler<GetAllInvoicesQuery, List<DTOs.InvoiceSummaryDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<GetAllInvoicesQueryHandler> _logger;

    public GetAllInvoicesQueryHandler(
        IInvoiceRepository invoiceRepository,
        ILogger<GetAllInvoicesQueryHandler> logger)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<DTOs.InvoiceSummaryDto>> Handle(GetAllInvoicesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all invoices with filters");

        var invoices = await _invoiceRepository.GetAllAsync(
            request.Page,
            request.PageSize,
            request.VendorName,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        return invoices.Select(invoice => new DTOs.InvoiceSummaryDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            VendorName = invoice.VendorName,
            TotalAmount = invoice.TotalAmount.Amount,
            Status = invoice.Status.ToString(),
            ProcessedAt = invoice.ProcessedAt
        }).ToList();
    }
}