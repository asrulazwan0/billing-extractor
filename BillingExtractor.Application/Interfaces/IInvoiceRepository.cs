using BillingExtractor.Domain.Entities;

namespace BillingExtractor.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByNumberAsync(string invoiceNumber, string vendorName, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? vendorName = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    Task<List<Invoice>> FindSimilarAsync(string invoiceNumber, string vendorName, DateTime invoiceDate, CancellationToken cancellationToken = default);
    Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string invoiceNumber, string vendorName, DateTime invoiceDate, CancellationToken cancellationToken = default);
}