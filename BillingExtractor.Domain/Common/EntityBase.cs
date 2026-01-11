namespace BillingExtractor.Domain.Common;

public abstract class EntityBase
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    
    private readonly List<DomainEventBase> _domainEvents = new();
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(DomainEventBase domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase other)
            return false;
            
        if (ReferenceEquals(this, other))
            return true;
            
        if (GetType() != other.GetType())
            return false;
            
        return Id == other.Id;
    }
    
    public override int GetHashCode() => Id.GetHashCode();
}

public abstract class DomainEventBase
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid Id { get; } = Guid.NewGuid();
}

public interface IAggregateRoot { }