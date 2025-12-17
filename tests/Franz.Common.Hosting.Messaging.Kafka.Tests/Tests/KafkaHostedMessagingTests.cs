using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;
using Franz.Common.Mediator.Dispatchers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class KafkaHostedMessagingTests
  : IClassFixture<KafkaHostingFixture>
{
  private readonly KafkaHostingFixture _fixture;

  public KafkaHostedMessagingTests(KafkaHostingFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task Kafka_hosted_listener_dispatches_event_through_mediator()
  {
    // Arrange
    TestEventHandler.Reset();

    using var scope = _fixture.Services.CreateScope();

    var dispatcher = scope.ServiceProvider
      .GetRequiredService<IDispatcher>();

    // Act
    await dispatcher.PublishEventAsync(new TestEvent("hello"));

    // Assert (deterministic, async-safe)
    var received = await TestEventHandler.Received.Task
      .WaitAsync(TimeSpan.FromSeconds(10));

    received.Should().NotBeNull();
    received.Value.Should().Be("hello");
  }
}
