using Confluent.Kafka;
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
  public async Task Kafka_hosted_listener_dispatches_event_through_mediator()
  {
    TestEventHandler.Reset();
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    await dispatcher.PublishEventAsync(new TestEvent("hello"));

    var received = await TestEventHandler.Received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    received.Value.Should().Be("hello");
  }

  [Fact]
  public async Task Kafka_hosted_listener_dispatches_event_to_all_handlers()
  {
    MultiHandlerProbe.Reset();
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    await dispatcher.PublishEventAsync(new FanoutTestEvent("fanout"));

    // Use the custom WaitAsync helper usually found in MultiHandlerProbes
    await MultiHandlerProbe.WaitAsync(expectedCount: 2, TimeSpan.FromSeconds(10));
    MultiHandlerProbe.Count.Should().Be(2);
  }

  [Fact]
  public async Task Kafka_event_should_trigger_all_registered_pipelines()
  {
    var pipelineProbe = _fixture.Services.GetRequiredService<ITestPipelineProbe>();
    var handlerProbe = _fixture.Services.GetRequiredService<ITestProbe>();
    pipelineProbe.Reset();
    handlerProbe.Reset();

    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();
    await dispatcher.PublishEventAsync(new TestEvent("pipeline-check"));

    await handlerProbe.CompletionTask.WaitAsync(TimeSpan.FromSeconds(10));
    pipelineProbe.WasExecuted.Should().BeTrue();
  }

  [Fact]
  public async Task Kafka_hosted_listener_recovers_messages_sent_while_offline()
  {
    await _fixture.StopHostAsync();

    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();
    var eventId = $"offline-{Guid.NewGuid()}";
    await dispatcher.PublishEventAsync(new TestEvent(eventId));

    TestEventHandler.Reset();
    await _fixture.StartHostAsync();

    var received = await TestEventHandler.Received.Task.WaitAsync(TimeSpan.FromSeconds(15));
    received.Value.Should().Be(eventId);
  }

  [Fact]
  public async Task Kafka_listener_remains_alive_after_poison_pill()
  {
    TestEventHandler.Reset();
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    // Act: Send raw garbage to the topic
    await _fixture.ProduceRawMessageAsync("TestEvent", "{ !!! broken json !!! }");

    // Act: Send valid message
    await dispatcher.PublishEventAsync(new TestEvent("survivor"));

    var received = await TestEventHandler.Received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    received.Value.Should().Be("survivor");
  }

  [Fact]
  public async Task Each_Kafka_message_should_be_processed_in_a_new_service_scope()
  {
    ScopeTrackingHandler.Reset();
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    await dispatcher.PublishEventAsync(new ScopeTestEvent("1"));
    await dispatcher.PublishEventAsync(new ScopeTestEvent("2"));

    await ScopeTrackingHandler.WaitAsync(2, TimeSpan.FromSeconds(10));
    ScopeTrackingHandler.ScopeIds.Distinct().Should().HaveCount(2);
  }

  [Fact]
  public async Task Kafka_listener_does_not_trigger_wrong_handler()
  {
    TestEventHandler.Reset();
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    await dispatcher.PublishEventAsync(new UnhandledTestEvent("secret"));

    var delayTask = Task.Delay(TimeSpan.FromSeconds(3));
    var completedTask = await Task.WhenAny(TestEventHandler.Received.Task, delayTask);

    completedTask.Should().Be(delayTask, "The handler should not have received this event.");
  }

  [Fact]
  public async Task IntegrationEvent_notification_does_not_fail_when_one_handler_throws()
  {
    FaultToleranceProbe.Reset();
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    await dispatcher.PublishNotificationAsync(new FaultToleranceTestEvent(),
        errorHandling: NotificationErrorHandling.ContinueOnError);

    var received = await FaultToleranceProbe.WaitAsync(TimeSpan.FromSeconds(5));
    received.Should().Be("boom");
  }

  [Fact]
  public async Task Kafka_listener_should_move_message_to_DLT_on_persistent_failure()
  {
    // Arrange
    var dltTopic = "franz-test-in-dlt"; // Based on your TopicNamer logic

    // Act: Send a message that triggers a handler that ALWAYS throws
    await _fixture.ProduceRawMessageAsync("TestEvent", "{ \"Fail\": true }");

    // Assert: Use the Native Consumer in your Fixture to listen to the DLT topic
    var dltConsumer = _fixture.Services.GetRequiredService<IConsumer<string, string>>();
    dltConsumer.Subscribe(dltTopic);

    var result = dltConsumer.Consume(TimeSpan.FromSeconds(15));

    result.Should().NotBeNull("Message should have been moved to the Dead Letter Topic.");
  }
}

