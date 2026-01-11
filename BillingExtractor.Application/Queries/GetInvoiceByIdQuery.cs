using MediatR;
using BillingExtractor.Application.DTOs;

namespace BillingExtractor.Application.Queries;

public class GetInvoiceByIdQuery : IRequest<InvoiceDto?>
{
    public Guid Id { get; }

    public GetInvoiceByIdQuery(Guid id)
    {
        Id = id;
    }
}