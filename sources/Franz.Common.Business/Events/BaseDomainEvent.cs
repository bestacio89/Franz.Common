using Franz.Common.Business.Events;
using Franz.Common.Mediator.Messages;

public abstract class BaseDomainEvent : IDomainEvent, INotification
{
  /// <summary>
  /// When the event was created (in UTC).
  /// </summary>
  public DateTimeOffset OccurredOn { get; protected set; } = DateTimeOffset.UtcNow;

  /// <summary>
  /// Unique identifier for this event instance (useful for deduplication/outbox).
  /// </summary>
  public Guid EventId { get; protected set; } = Guid.NewGuid();

  /// <summary>
  /// Correlation/trace identifier 
  /// </summary>
  public string? CorrelationId { get; set; }

  /// <summary>
  /// Aggregate root identifier that raised this event
  /// </summary>
  public string? AggregateId { get; set; }
}
