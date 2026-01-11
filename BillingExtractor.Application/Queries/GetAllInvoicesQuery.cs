using MediatR;
using BillingExtractor.Application.DTOs;

namespace BillingExtractor.Application.Queries;

public class GetAllInvoicesQuery : IRequest<List<InvoiceSummaryDto>>
{
    public int Page { get; }
    public int PageSize { get; }
    public string? VendorName { get; }
    public DateTime? FromDate { get; }
    public DateTime? ToDate { get; }

    public GetAllInvoicesQuery(int page = 1, int pageSize = 20, string? vendorName = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        Page = page;
        PageSize = pageSize;
        VendorName = vendorName;
        FromDate = fromDate;
        ToDate = toDate;
    }
}