#nullable enable

using Franz.Common.Mediator;

namespace Franz.Common.Messaging.Sagas.Tests;

/// <summary>
/// Null-object publisher for saga tests.
/// Satisfies IMessagingPublisher without emitting anything.
/// </summary>
public sealed class NullMessagingPublisher : IMessagingPublisher
{
  public static readonly NullMessagingPublisher Instance = new();

  private NullMessagingPublisher() { }

  public Task Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent)
    where TIntegrationEvent : IIntegrationEvent
    => Task.CompletedTask;
}
