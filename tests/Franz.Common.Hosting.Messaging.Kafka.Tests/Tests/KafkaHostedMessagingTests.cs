using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Fakes;
using Franz.Common.Hosting.Messaging.Kafka.Tests.Handlers;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class KafkaHostedMessagingTests
  : IClassFixture<KafkaContainerFixture>
{
  private readonly KafkaContainerFixture _kafka;

  public KafkaHostedMessagingTests(KafkaContainerFixture kafka)
  {
    _kafka = kafka;
  }

  [Fact]
  public async Task Kafka_hosted_listener_dispatches_event_through_mediator()
  {
    // Arrange
    TestEventHandler.Reset();

    await using var hostFixture =
        new KafkaHostingFixture(_kafka.BootstrapServers);

    await hostFixture.StartAsync();

    var producer = hostFixture.Host.Services.GetRequiredService<IMessagingSender>();
    var serializer = hostFixture.Host.Services.GetRequiredService<IMessageSerializer>();

    var evt = new TestEvent("hello-franz");
    var message = TestMessageFactory.FromEvent(evt, serializer);

    // Act
    await producer.SendAsync(message);

    // Assert
    var received = await TestEventHandler.Received.Task
        .WaitAsync(TimeSpan.FromSeconds(10));

    Assert.Equal(evt.Value, received.Value);
  }
}
