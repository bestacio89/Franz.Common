namespace Franz.Common.Messaging.Configuration;

/// <summary>
/// Selects the message identity used to preserve relative processing order.
/// The transport adapter determines how the selected identity maps to broker ordering.
/// </summary>
public enum MessageOrderingStrategy
{
  /// <summary>No ordering key is required by the application contract.</summary>
  None,

  /// <summary>Order messages by their unique message identifier.</summary>
  MessageId,

  /// <summary>Order messages sharing a correlation identifier together.</summary>
  CorrelationId,

  /// <summary>Order messages belonging to the same aggregate together.</summary>
  AggregateId
}
