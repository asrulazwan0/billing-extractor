using BillingExtractor.Domain.Common;

namespace BillingExtractor.Domain.Events;

public class InvoiceCreatedEvent : DomainEventBase
{
    public Guid InvoiceId { get; }
    
    public InvoiceCreatedEvent(Guid invoiceId)
    {
        InvoiceId = invoiceId;
    }
}

public class InvoiceProcessingEvent : DomainEventBase
{
    public Guid InvoiceId { get; }
    
    public InvoiceProcessingEvent(Guid invoiceId)
    {
        InvoiceId = invoiceId;
    }
}

public class InvoiceProcessedEvent : DomainEventBase
{
    public Guid InvoiceId { get; }
    
    public InvoiceProcessedEvent(Guid invoiceId)
    {
        InvoiceId = invoiceId;
    }
}

public class InvoiceFailedEvent : DomainEventBase
{
    public Guid InvoiceId { get; }
    public string Error { get; }
    
    public InvoiceFailedEvent(Guid invoiceId, string error)
    {
        InvoiceId = invoiceId;
        Error = error;
    }
}

public class InvoiceValidationWarningEvent : DomainEventBase
{
    public Guid InvoiceId { get; }
    public string Code { get; }
    public string Message { get; }
    
    public InvoiceValidationWarningEvent(Guid invoiceId, string code, string message)
    {
        InvoiceId = invoiceId;
        Code = code;
        Message = message;
    }
}

public class InvoiceValidationErrorEvent : DomainEventBase
{
    public Guid InvoiceId { get; }
    public string Code { get; }
    public string Message { get; }
    
    public InvoiceValidationErrorEvent(Guid invoiceId, string code, string message)
    {
        InvoiceId = invoiceId;
        Code = code;
        Message = message;
    }
}