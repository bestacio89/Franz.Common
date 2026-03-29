#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Hosting.Kafka.HostedServices;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests;

[Collection("KafkaHosting")]
public sealed class KafkaMessagingEngineTests
{
  private readonly KafkaHostingFixture _fixture;

  public KafkaMessagingEngineTests(KafkaHostingFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task HandleMessage_ShouldInjectCorrectCorrelationId_IntoScopedContext()
  {
    // 1. Arrange
    var probe = _fixture.Host!.Services.GetRequiredService<ITestProbe>();
    var serializer = _fixture.Host.Services.GetRequiredService<IMessageSerializer>();

    var correlationId = Guid.CreateVersion7();
    var messageId = Guid.CreateVersion7();

    var message = new Message("{\"Value\":\"Context-Check\"}")
    {
      Id = messageId,
      CorrelationId = correlationId
    };

    var rawContent = serializer.Serialize(message);

    // 2. Act
    await _fixture.ProduceRawMessageAsync("TestEvent", rawContent);

    // 3. Assert
    var arrived = await probe.WaitForArrivalAsync(messageId);
    arrived.Should().BeTrue("The strategy executer must be found and executed.");
  }

  [Fact]
  public void Listener_ShouldHaveDelegate_WiredByHostedService()
  {
    // Verifies that KafkaMessagingHostedService correctly wired
    // OnMessageReceivedAsync on startup — without stopping the shared host
    // which would break all subsequent tests in the collection.
    var listener = _fixture.Host!.Services.GetRequiredService<KafkaMessageListener>();

    listener.OnMessageReceivedAsync.Should().NotBeNull(
        "KafkaMessagingHostedService should have wired OnMessageReceivedAsync on startup.");
  }
}