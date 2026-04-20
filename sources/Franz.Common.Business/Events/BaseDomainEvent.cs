using Franz.Common.Mediator.Messages;
namespace Franz.Common.Business.Events;
public interface IDomainEvent : IEvent
{
  Guid EventId { get; }

  Guid? CorrelationId { get; }
  Guid? AggregateId { get; }
  string AggregateType { get; }
  string EventType { get; }
}
