#nullable enable
using System.Text;
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
    var probe = _fixture.Host!.Services.GetRequiredService<ITestProbe>();
    var serializer = _fixture.Host.Services.GetRequiredService<IMessageSerializer>();
    var correlationId = Guid.CreateVersion7();
    var messageId = Guid.CreateVersion7();

    var message = new Message("{\"Value\":\"Context-Check\"}")
    {
      Id = messageId,
      CorrelationId = correlationId
    };

    await _fixture.ProduceRawMessageAsync("TestEvent", serializer.Serialize(message));

    var arrived = await probe.WaitForArrivalAsync(messageId);
    arrived.Should().BeTrue("the strategy executer must be found and executed.");
  }

  [Fact]
  public async Task HandlerFailure_ShouldPublishOriginalPayloadToDeadLetterTopic()
  {
    var serializer = _fixture.Host!.Services.GetRequiredService<IMessageSerializer>();
    var message = new Message("__fail__") { Id = Guid.CreateVersion7() };
    var payload = serializer.Serialize(message);

    await _fixture.ProduceRawMessageAsync("TestEvent", payload);

    var deadLetter = await _fixture.ConsumeDeadLetterAsync(
        "hero-test-dlt", payload, TimeSpan.FromSeconds(10));

    deadLetter.Should().NotBeNull("the original record must be preserved after retries are exhausted");
    Encoding.UTF8.GetString(deadLetter!.Message.Value).Should().Be(payload);
    deadLetter.Message.Headers.Any(h => h.Key == "x-franz-original-topic").Should().BeTrue();
    deadLetter.Message.Headers.Any(h => h.Key == "x-franz-failure-type").Should().BeTrue();
    deadLetter.Message.Headers
        .First(h => h.Key == "x-franz-original-topic")
        .GetValueBytes()
        .Should()
        .Equal(Encoding.UTF8.GetBytes("TestEvent"));
  }

  [Fact]
  public void Listener_ShouldHaveDelegate_WiredByHostedService()
  {
    var listener = _fixture.Host!.Services.GetRequiredService<KafkaMessageListener>();
    listener.OnMessageReceivedAsync.Should().NotBeNull(
        "KafkaMessagingHostedService should wire the callback before listening.");
  }
}
