using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BillingExtractor.Infrastructure.LLM;

public class MockLLMService : BaseLLMService
{
    public MockLLMService(ILogger<MockLLMService> logger) : base(logger)
    {
    }

    public override async Task<InvoiceDto> ExtractInvoiceAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock extraction for file: {FileName}", fileName);

        // Simulate processing delay
        await Task.Delay(500, cancellationToken);

        // Generate mock invoice data
        var random = new Random();
        var invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{random.Next(1000, 9999)}";
        var vendors = new[] { "Fresh Foods Inc.", "Quality Produce Co.", "Global Suppliers Ltd.", "Local Farm Distributors" };
        var items = new[]
        {
            ("Apples", "kg"),
            ("Oranges", "kg"),
            ("Bananas", "bunch"),
            ("Tomatoes", "kg"),
            ("Potatoes", "kg"),
            ("Onions", "kg"),
            ("Carrots", "kg"),
            ("Lettuce", "head"),
            ("Milk", "liter"),
            ("Eggs", "dozen")
        };

        var vendor = vendors[random.Next(vendors.Length)];
        var lineItemsCount = random.Next(3, 8);
        var lineItems = new List<LineItemDto>();

        for (int i = 0; i < lineItemsCount; i++)
        {
            var (description, unit) = items[random.Next(items.Length)];
            var quantity = random.Next(1, 51);
            var unitPrice = Math.Round((decimal)(random.NextDouble() * 10 + 1), 2);
            var lineTotal = quantity * unitPrice;

            lineItems.Add(new LineItemDto
            {
                LineNumber = i + 1,
                Description = description,
                Quantity = quantity,
                Unit = unit,
                UnitPrice = unitPrice,
                LineTotal = lineTotal
            });
        }

        var totalAmount = lineItems.Sum(li => li.LineTotal);
        var taxAmount = Math.Round(totalAmount * 0.1m, 2);
        var subtotal = totalAmount - taxAmount;

        var invoice = new InvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.Now.AddDays(-random.Next(1, 30)),
            DueDate = DateTime.Now.AddDays(random.Next(15, 45)),
            VendorName = vendor,
            CustomerName = "Your Grocery Store",
            Currency = "USD",
            TotalAmount = totalAmount,
            TaxAmount = taxAmount,
            Subtotal = subtotal,
            Status = "Extracted",
            ProcessedAt = DateTime.UtcNow,
            LineItems = lineItems
        };

        // Add some random validation warnings
        if (random.NextDouble() > 0.7)
        {
            invoice.ValidationWarnings.Add(new ValidationWarningDto
            {
                Code = "MOCK_WARNING",
                Message = "Mock: This is a simulated validation warning"
            });
        }

        return invoice;
    }
}