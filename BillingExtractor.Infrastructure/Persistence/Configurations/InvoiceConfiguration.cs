using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BillingExtractor.Domain.Entities;

namespace BillingExtractor.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.VendorName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.CustomerName)
            .HasMaxLength(200);

        builder.Property(i => i.OriginalFileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.FileHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.FilePath)
            .HasMaxLength(1000);

        builder.Property(i => i.ProcessingError)
            .HasMaxLength(2000);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.InvoiceDate)
            .IsRequired();

        builder.Property(i => i.ProcessedAt)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(i => i.InvoiceNumber);
        builder.HasIndex(i => i.VendorName);
        builder.HasIndex(i => i.InvoiceDate);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.FileHash).IsUnique();

        // Composite index for duplicate detection
        builder.HasIndex(i => new { i.InvoiceNumber, i.VendorName, i.InvoiceDate });

        // Configure relationships
        builder.HasMany(i => i.LineItems)
            .WithOne(li => li.Invoice)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}