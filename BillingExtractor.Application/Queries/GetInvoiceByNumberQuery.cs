using MediatR;
using BillingExtractor.Application.DTOs;

namespace BillingExtractor.Application.Queries;

public class GetInvoiceByNumberQuery : IRequest<InvoiceDto?>
{
    public string InvoiceNumber { get; }
    public string VendorName { get; }

    public GetInvoiceByNumberQuery(string invoiceNumber, string vendorName)
    {
        InvoiceNumber = invoiceNumber ?? throw new ArgumentNullException(nameof(invoiceNumber));
        VendorName = vendorName ?? throw new ArgumentNullException(nameof(vendorName));
    }
}