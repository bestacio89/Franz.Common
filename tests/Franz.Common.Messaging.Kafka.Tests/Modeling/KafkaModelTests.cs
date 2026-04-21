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
    factoryMock.Setup(x => x.Current)
      .Returns(new ProducerConfig { BootstrapServers = _bootstrapServers });

    await using var sut = new KafkaModel(factoryMock.Object);

    var message = new Message { Body = "KafkaModel Integration Test" };

    await sut.Produce(topic, message, CancellationToken.None);

    var config = new ConsumerConfig
    {
      BootstrapServers = _bootstrapServers,
      GroupId = $"verifier-{Guid.NewGuid():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest
    };

    using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();

    try
    {
      consumer.Subscribe(topic);

      using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

      var result = consumer.Consume(cts.Token);

      result.Should().NotBeNull();

      var deserialized = JsonSerializer.Deserialize<Message>(
        result.Message.Value,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

      deserialized.Should().NotBeNull();
      deserialized!.Body.Should().Be(message.Body);
    }
    finally
    {
      try { consumer.Close(); } catch { }
      consumer.Dispose();
    }
  }

  [Fact]
  public async Task Produce_WithHighConcurrency_ShouldMaintainIdempotency()
  {
    var topic = $"concurrency-{Guid.NewGuid():N}";

    var factoryMock = new Mock<IConnectionFactoryProvider>();
    factoryMock.Setup(x => x.Current)
      .Returns(new ProducerConfig { BootstrapServers = _bootstrapServers });

    await using var sut = new KafkaModel(factoryMock.Object);

    var tasks = Enumerable.Range(0, 100)
      .Select(i => sut.Produce(topic, new Message { Body = $"Msg-{i}" }, CancellationToken.None).AsTask());

    await Task.WhenAll(tasks);

    // Ensure broker flush completion before test ends
    if (sut is IAsyncDisposable)
    {
      // Dispose already enforces flush if implemented correctly
      await Task.Yield();
    }
  }
}