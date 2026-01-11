using Microsoft.EntityFrameworkCore;
using BillingExtractor.Domain.Entities;
using BillingExtractor.Infrastructure.Persistence.Configurations;

namespace BillingExtractor.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<LineItem> LineItems => Set<LineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new LineItemConfiguration());

        // Configure value objects
        modelBuilder.Entity<Invoice>().OwnsOne(i => i.TotalAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
        });

        modelBuilder.Entity<Invoice>().OwnsOne(i => i.TaxAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TaxAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("TaxCurrency").HasMaxLength(3);
        });

        modelBuilder.Entity<Invoice>().OwnsOne(i => i.Subtotal, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Subtotal").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("SubtotalCurrency").HasMaxLength(3);
        });

        modelBuilder.Entity<LineItem>().OwnsOne(li => li.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("UnitCurrency").HasMaxLength(3);
        });

        modelBuilder.Entity<LineItem>().OwnsOne(li => li.LineTotal, money =>
        {
            money.Property(m => m.Amount).HasColumnName("LineTotal").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("LineCurrency").HasMaxLength(3);
        });

        // Configure JSON serialization for collections
        modelBuilder.Entity<Invoice>()
            .OwnsMany(i => i.ValidationWarnings, vw =>
            {
                vw.ToJson();
                vw.Property(v => v.Code).HasMaxLength(50);
                vw.Property(v => v.Message).HasMaxLength(500);
            });

        modelBuilder.Entity<Invoice>()
            .OwnsMany(i => i.ValidationErrors, ve =>
            {
                ve.ToJson();
                ve.Property(v => v.Code).HasMaxLength(50);
                ve.Property(v => v.Message).HasMaxLength(500);
            });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await DispatchDomainEventsAsync();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker
            .Entries<Domain.Common.EntityBase>()
            .Where(x => x.Entity.DomainEvents?.Any() == true)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        // In a real application, you would publish these events through a mediator
        // For simplicity, we'll just log them
        foreach (var domainEvent in domainEvents)
        {
            Console.WriteLine($"Domain Event: {domainEvent.GetType().Name} occurred at {domainEvent.OccurredOn}");
        }
    }
}