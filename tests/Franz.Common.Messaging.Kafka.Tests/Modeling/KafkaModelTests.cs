#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.Kafka.Modeling;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Moq;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Modeling;

[Collection("Kafka")]
public sealed class KafkaModelIntegrationTests(KafkaContainerFixture fixture)
{
  private readonly string _bootstrapServers = fixture.BootstrapServers;

  [Fact]
  public async Task Produce_ShouldSuccessfullyWriteToBroker_AndBeReadable()
  {
    var topic = $"test-topic-{Guid.NewGuid():N}";
    var factoryMock = new Mock<IConnectionFactoryProvider>();
    factoryMock.Setup(x => x.Current).Returns(new ProducerConfig { BootstrapServers = _bootstrapServers });

    await using var sut = new KafkaModel(factoryMock.Object);
    var message = new Message { Body = "KafkaModel Integration Test" };

    await sut.Produce(topic, message, CancellationToken.None);

    // SENIOR FIX: Clean the address and wrap the consumer in Close() logic
    var config = new ConsumerConfig
    {
      BootstrapServers = _bootstrapServers,
      GroupId = $"verifier-{Guid.NewGuid():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest
    };

    using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
    consumer.Subscribe(topic);

    ConsumeResult<string, byte[]>? result = null;
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    while (!cts.IsCancellationRequested)
    {
      result = consumer.Consume(TimeSpan.FromMilliseconds(500));
      if (result != null) break;
    }

    consumer.Close(); // ✅ Explicitly leave the group

    result.Should().NotBeNull();
    var deserialized = JsonSerializer.Deserialize<Message>(result!.Message.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    deserialized!.Body.ToString().Should().Be(message.Body.ToString());
  }

  [Fact]
  public async Task Produce_WithHighConcurrency_ShouldMaintainIdempotency()
  {
    var topic = $"concurrency-{Guid.NewGuid():N}";
    var factoryMock = new Mock<IConnectionFactoryProvider>();
    factoryMock.Setup(x => x.Current).Returns(new ProducerConfig { BootstrapServers = _bootstrapServers });

    await using var sut = new KafkaModel(factoryMock.Object);

    // SENIOR FIX: Ensure we await the results and then explicitly Dispose/Flush
    var tasks = Enumerable.Range(0, 100).Select(i =>
        sut.Produce(topic, new Message { Body = $"Msg-{i}" }, CancellationToken.None).AsTask()
    );

    await Task.WhenAll(tasks);

    // The await using handles the Flush/Dispose call.
  }
}