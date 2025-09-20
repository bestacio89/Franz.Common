using Franz.Common.Business.Events;
using Franz.Common.Mediator.Messages;
using System.Diagnostics;

namespace Franz.Common.Business.Domain;

[DebuggerDisplay("{EventType} (Aggregate={AggregateId}, Correlation={CorrelationId})")]
public abstract class BaseDomainEvent : IDomainEvent, INotification
{
  /// <summary>
  /// Unique identifier for this event instance (useful for deduplication/outbox).
  /// </summary>
  public Guid EventId { get; init; } = Guid.NewGuid();

  /// <summary>
  /// When the event was created (in UTC).
  /// </summary>
  public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

  /// <summary>
  /// Correlation/trace identifier (for distributed tracing).
  /// </summary>
  public string? CorrelationId { get; init; }

  /// <summary>
  /// Aggregate root identifier that raised this event.
  /// </summary>
  public Guid? AggregateId { get; init; }

  /// <summary>
  /// Type of aggregate root (e.g. "Order", "Customer").
  /// </summary>
  public string AggregateType { get; init; } = string.Empty;

  /// <summary>
  /// Event type name (by default, the class name).
  /// Useful for serialization/logging.
  /// </summary>
  public string EventType => GetType().Name;
}
