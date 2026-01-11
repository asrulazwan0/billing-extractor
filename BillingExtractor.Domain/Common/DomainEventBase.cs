namespace BillingExtractor.Domain.Common;

public abstract class DomainEventBase
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid Id { get; } = Guid.NewGuid();
}