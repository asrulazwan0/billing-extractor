using BillingExtractor.Domain.Events;

namespace BillingExtractor.Domain.Common;

public abstract class EntityBase
{
    private List<DomainEventBase>? _domainEvents;
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    public IReadOnlyList<DomainEventBase>? DomainEvents => _domainEvents?.AsReadOnly();

    public void AddDomainEvent(DomainEventBase domainEvent)
    {
        _domainEvents ??= new List<DomainEventBase>();
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(DomainEventBase domainEvent)
    {
        _domainEvents?.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}