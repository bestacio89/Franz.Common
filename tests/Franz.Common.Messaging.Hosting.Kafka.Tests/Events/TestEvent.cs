#nullable enable
using Franz.Common.Messaging.Messages;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Events;

/// <summary>
/// A complete test event that bridges the Mediator (IEvent) and 
/// Messaging Transport (Message) contracts.
/// </summary>
public sealed class TestEvent : Message, IEvent
{
  // ✅ Use GuidV7 for chronological sorting and tracking in TestProbes
  public TestEvent()
  {
    Id = Guid.CreateVersion7();
  }

  public string Value { get; init; } = string.Empty;

  public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}