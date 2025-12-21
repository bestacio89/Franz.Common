using FluentAssertions;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Probes;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Franz.Common.Mediator.Dispatchers.DispatchingStrategies;

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
  public async Task IntegrationEvent_notification_does_not_fail_when_one_handler_throws()
  {
    FaultToleranceProbe.Reset();

    using var scope = _fixture.Services.CreateScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

    // Act
    await dispatcher.PublishNotificationAsync(new FaultToleranceTestEvent(),
  errorHandling: NotificationErrorHandling.ContinueOnError); // IMPORTANT: PublishAsync (notification), not PublishEventAsync

    // Assert
    var received = await FaultToleranceProbe.WaitAsync(TimeSpan.FromSeconds(2));
    received.Should().Be("boom");
  }


  [Fact]
  public async Task Kafka_pipeline_continues_after_handler_failure()
  {
    FaultToleranceProbe.Reset();
    MultiHandlerProbe.Reset();

    using var scope = _fixture.Services.CreateScope();
    var publisher = scope.ServiceProvider.GetRequiredService<IMessagingPublisher>();

    // 1️⃣ publish message that causes a handler failure
    await publisher.Publish(new FaultToleranceTestEvent());

    // Wait until the failure was *handled*
    await FaultToleranceProbe.WaitAsync(TimeSpan.FromSeconds(10));

    // 2️⃣ publish a second message
    await publisher.Publish(new FanoutTestEvent2("still-alive"));

    // Wait until the second message is *processed*
    await MultiHandlerProbe.WaitAsync(TimeSpan.FromSeconds(10));

    // Assert semantic outcome
    MultiHandlerProbe.Count.Should().BeGreaterThanOrEqualTo(2);
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
