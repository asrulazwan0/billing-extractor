using Microsoft.EntityFrameworkCore;
using BillingExtractor.Application.Interfaces;
using BillingExtractor.Domain.Entities;
using BillingExtractor.Infrastructure.Persistence;

namespace BillingExtractor.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ApplicationDbContext _context;

    public InvoiceRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByNumberAsync(string invoiceNumber, string vendorName, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i =>
                i.InvoiceNumber == invoiceNumber &&
                i.VendorName == vendorName,
                cancellationToken);
    }

    public async Task<Invoice?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.FileHash == fileHash, cancellationToken);
    }

    public async Task<List<Invoice>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? vendorName = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices
            .Include(i => i.LineItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(vendorName))
        {
            query = query.Where(i => i.VendorName.Contains(vendorName));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= toDate.Value);
        }

        query = query.OrderByDescending(i => i.ProcessedAt);

        if (page > 0 && pageSize > 0)
        {
            query = query.Skip((page - 1) * pageSize).Take(pageSize);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<Invoice>> FindSimilarAsync(string invoiceNumber, string vendorName, DateTime invoiceDate, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Where(i => i.InvoiceNumber == invoiceNumber &&
                       i.VendorName == vendorName &&
                       i.InvoiceDate.Date == invoiceDate.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _context.Invoices.Update(invoice);
        await Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetByIdAsync(id, cancellationToken);
        if (invoice == null) return false;

        _context.Invoices.Remove(invoice);
        return true;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string invoiceNumber, string vendorName, DateTime invoiceDate, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .AnyAsync(i =>
                i.InvoiceNumber == invoiceNumber &&
                i.VendorName == vendorName &&
                i.InvoiceDate.Date == invoiceDate.Date,
                cancellationToken);
    }
}