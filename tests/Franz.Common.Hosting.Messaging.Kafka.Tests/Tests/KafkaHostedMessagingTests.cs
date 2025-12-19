using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;
using Franz.Common.Mediator.Dispatchers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;

public sealed class KafkaHostedMessagingTests
  : IClassFixture<KafkaHostingFixture>
{
  private readonly KafkaHostingFixture _fixture;

  public KafkaHostedMessagingTests(KafkaHostingFixture fixture)
  {
    _fixture = fixture;
  }
  [Fact]
  public async Task Kafka_hosted_listener_ignores_event_with_no_handler()
  {
    using var scope = _fixture.Services.CreateScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

    Func<Task> act = () => dispatcher.PublishEventAsync(
      new UnhandledTestEvent("noop"));

    await act.Should().NotThrowAsync();
  }
  [Fact]
  public async Task Kafka_hosted_listener_dispatches_event_to_all_handlers()
  {
    MultiHandlerProbe.Reset();

    using var scope = _fixture.Services.CreateScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

    await dispatcher.PublishEventAsync(new FanoutTestEvent("fanout"));

    await MultiHandlerProbe.WaitAsync(TimeSpan.FromSeconds(10));

    MultiHandlerProbe.Count.Should().Be(2);
  }


  [Fact]
  public async Task Kafka_hosted_listener_continues_when_one_handler_fails()
  {
    // Arrange
    FaultToleranceProbe.Reset();

    using var scope = _fixture.Services.CreateScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

    // Act
    await dispatcher.PublishEventAsync(new FaultToleranceTestEvent("still-alive"));

    // Assert
    var received = await FaultToleranceProbe
      .WaitAsync(TimeSpan.FromSeconds(10));

    received.Should().Be("still-alive");
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
