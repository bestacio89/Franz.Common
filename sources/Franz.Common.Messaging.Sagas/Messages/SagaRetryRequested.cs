#nullable enable

using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Sagas.Messages;

/// <summary>
/// Internal integration-event envelope used to resume one saga without replaying the
/// original business event to unrelated broker subscribers.
/// </summary>
public sealed record SagaRetryRequested : IIntegrationEvent
{
  public Guid SagaId { get; init; }
  public Guid OriginalMessageId { get; init; }
  public string OriginalMessageType { get; init; } = string.Empty;
  public string Body { get; init; } = string.Empty;
  public Dictionary<string, string[]> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
  public Dictionary<string, string> Properties { get; init; } = new(StringComparer.OrdinalIgnoreCase);
  public DateTimeOffset OccurredOn { get; init; }
  public Guid? AggregateId { get; init; }
  public MessageKind Kind { get; init; } = MessageKind.IntegrationEvent;
}