using BillingExtractor.Domain.Common;
using BillingExtractor.Domain.ValueObjects;

namespace BillingExtractor.Domain.Entities;

public class LineItem : EntityBase
{
    public int LineNumber { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public Money UnitPrice { get; private set; } = null!; // EF Core will set this
    public Money LineTotal { get; private set; } = null!; // EF Core will set this
    
    // Navigation property
    public Guid InvoiceId { get; private set; }
    public Invoice Invoice { get; private set; } = null!;
    
    // Private constructor for EF Core
    private LineItem() { }
    
    public static LineItem Create(
        int lineNumber,
        string description,
        decimal quantity,
        string unit,
        Money unitPrice,
        Guid invoiceId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));
        
        if (unitPrice.Amount < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        
        var lineTotal = new Money(quantity * unitPrice.Amount, unitPrice.Currency);
        
        return new LineItem
        {
            LineNumber = lineNumber,
            Description = description.Trim(),
            Quantity = quantity,
            Unit = unit.Trim(),
            UnitPrice = unitPrice,
            LineTotal = lineTotal,
            InvoiceId = invoiceId
        };
    }
}