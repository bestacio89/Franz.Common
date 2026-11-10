namespace Franz.Common.Messaging.Messages;

/// <summary>
/// Opt-in contract for message payloads whose correlation identifier can be hydrated without reflection.
/// </summary>
public interface ICorrelatedMessage
{
  Guid? CorrelationId { get; set; }
}
