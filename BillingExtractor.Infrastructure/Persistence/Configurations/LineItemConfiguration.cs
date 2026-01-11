using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BillingExtractor.Domain.Entities;

namespace BillingExtractor.Infrastructure.Persistence.Configurations;

public class LineItemConfiguration : IEntityTypeConfiguration<LineItem>
{
    public void Configure(EntityTypeBuilder<LineItem> builder)
    {
        builder.HasKey(li => li.Id);

        builder.Property(li => li.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(li => li.Unit)
            .HasMaxLength(50);

        builder.Property(li => li.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(li => li.LineNumber)
            .IsRequired();
    }
}