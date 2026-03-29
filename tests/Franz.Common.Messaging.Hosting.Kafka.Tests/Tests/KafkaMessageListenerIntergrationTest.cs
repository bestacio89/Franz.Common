#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests;

[Collection("KafkaHosting")] // Reuse the hardened KRaft collection

public sealed class KafkaListenerIntegrationTests
{
  private readonly KafkaHostingFixture _fixture;


  public KafkaListenerIntegrationTests(KafkaHostingFixture fixture )  {
    _fixture = fixture;
    
  }

  [Fact]
  public async Task Listener_ShouldProcessMessageAndCommit_OnlyAfterSuccessfulHandling()
  {
    // 1. Arrange

    var probe = _fixture.Host!.Services.GetRequiredService<ITestProbe>();
    var messageId = Guid.CreateVersion7();
    var serializer = _fixture.Host.Services.GetRequiredService<IMessageSerializer>();
    var message = new Message("{\"Data\":\"Verified\"}")
    {
      Id = messageId,
      CorrelationId = Guid.CreateVersion7()
    };

    var rawJson = serializer.Serialize(message);

    // 2. Act
    await _fixture.ProduceRawMessageAsync("TestEvent", rawJson);

    // 3. Assert
    var arrived = await probe.WaitForArrivalAsync(messageId, TimeSpan.FromSeconds(20));
    arrived.Should().BeTrue("The listener must deserialize via IMessageSerializer and await the async handler.");
  }

  [Fact]
  public async Task Listener_ShouldHandleMalformedJson_WithoutCrashingTheProcess()
  {
    // 1. Arrange
    var probe = _fixture.Host!.Services.GetRequiredService<ITestProbe>();
    var validId = Guid.CreateVersion7();
    var serializer = _fixture.Host.Services.GetRequiredService<IMessageSerializer>();
    var malformedJson = "{ invalid json }"; // intentionally broken
    var validMessage = new Message("{\"Body\":\"{}\"}")
    {
      Id = validId,
      CorrelationId = Guid.CreateVersion7()
    };
    var validJson = serializer.Serialize(validMessage);

    // 2. Act
    // Produce malformed first, then valid
    await _fixture.ProduceRawMessageAsync("TestEvent", malformedJson);
    await _fixture.ProduceRawMessageAsync("TestEvent", validJson);

    // 3. Assert
    var arrived = await probe.WaitForArrivalAsync(validId, TimeSpan.FromSeconds(20));
    arrived.Should().BeTrue("The listener must be resilient to deserialization failures and move to the next offset.");
  }
}