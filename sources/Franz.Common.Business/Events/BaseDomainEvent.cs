using Franz.Common.Mediator.Messages;
namespace Franz.Common.Business.Events;
public interface IDomainEvent : IEvent
{
  Guid EventId { get; }
  new DateTimeOffset OccurredOn { get; }
  string? CorrelationId { get; }
  Guid? AggregateId { get; }
  string AggregateType { get; }
  string EventType { get; }
}
