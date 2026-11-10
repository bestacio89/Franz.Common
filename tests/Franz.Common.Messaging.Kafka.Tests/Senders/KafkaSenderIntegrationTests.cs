#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Transactions;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Senders;

[Collection("KafkaIntegration")]
public sealed class KafkaSenderTests : IClassFixture<KafkaContainerFixture>, IAsyncDisposable
{
  private readonly KafkaContainerFixture _fixture;
  private readonly IServiceProvider _serviceProvider;
  private readonly IServiceScope _scope;

  private readonly KafkaSender _sut;
  

  public KafkaSenderTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture;

    _serviceProvider = fixture.BuildServiceProvider();
    _scope = _serviceProvider.CreateScope();

    _sut = _scope.ServiceProvider.GetServices<IMessagingSender>()
        .OfType<KafkaSender>()
        .SingleOrDefault()
        ?? throw new InvalidOperationException("KafkaSender not found in DI.");
  }

  [Fact]
  public async Task SendAsync_ValidMessage_DeliversSuccessfullyToKafka()
  {
    var messageId = Guid.NewGuid();
    var correlationId = Guid.NewGuid();

    var message = new Message
    {
      Id = messageId,
      CorrelationId = correlationId,
      Body = "Integration-Test-Data",
      Headers = new Dictionary<string, string[]>
      {
        { "Custom-Header", new[] { "Value1" } }
      }
    };

    var topic = _scope.ServiceProvider.GetRequiredService<KafkaTopicNames>().TopicName;

    using var consumer = CreateTestConsumer();

    try
    {
      consumer.Subscribe(topic);

      await _sut.SendAsync(message);

      using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

      var result = consumer.Consume(cts.Token);

      result.Should().NotBeNull();

      result!.Message.Key.Should().Be(messageId.ToString());

      var payload = Encoding.UTF8.GetString(result.Message.Value);
      payload.Should().Contain("Integration-Test-Data");

      var roundTrippedMessage = _scope.ServiceProvider
          .GetRequiredService<IMessageSerializer>()
          .Deserialize<Message>(payload);

      roundTrippedMessage.Should().NotBeNull();
      roundTrippedMessage!.Id.Should().Be(messageId);
      roundTrippedMessage.CorrelationId.Should().Be(correlationId);
      roundTrippedMessage.Body.Should().Be("Integration-Test-Data");
      roundTrippedMessage.Headers.Should().ContainKey(Message.CorrelationHeader)
          .WhoseValue.Should().ContainSingle(correlationId.ToString("D"));
      roundTrippedMessage.Headers.Should().ContainKey("Custom-Header")
          .WhoseValue.Should().ContainSingle("Value1");

      var headers = result.Message.Headers.ToDictionary(
        h => h.Key,
        h => Encoding.UTF8.GetString(h.GetValueBytes()));

      headers["X-Message-ID"].Should().Be(messageId.ToString());
      headers["Custom-Header"].Should().Be("Value1");
    }
    finally
    {
      try { consumer.Close(); } catch { }
      consumer.Dispose(); // 🔥 FIX: proper Kafka cleanup
    }
  }

  [Fact]
  public async Task SendAsync_NullMessage_ThrowsArgumentNullException()
  {
    Func<Task> act = async () => await _sut.SendAsync(null!);

    await act.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("message");
  }

  [Fact]
  public async Task SendAsync_WhenDisposed_ThrowsObjectDisposedException()
  {
    var message = new Message
    {
      Id = Guid.NewGuid(),
      CorrelationId = Guid.NewGuid(),
      Body = "Test"
    };

    await _sut.DisposeAsync();

    Func<Task> act = async () => await _sut.SendAsync(message);

    await act.Should().ThrowAsync<ObjectDisposedException>()
      .WithMessage($"*{nameof(KafkaSender)}*");
  }

  [Fact]
  public async Task SendAsync_OnProduceException_Rethrows()
  {
    var message = new Message
    {
      Id = Guid.NewGuid(),
      CorrelationId = Guid.NewGuid(),
      Body = "Test"
    };

    var mockProducer = new Mock<IProducer<string, byte[]>>();
    mockProducer
      .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new ProduceException<string, byte[]>(
        new Error(ErrorCode.Local_MsgTimedOut, "Simulated"), null!));

    var sut = new KafkaSender(
      mockProducer.Object,
      _scope.ServiceProvider.GetRequiredService<IMessageSerializer>(),
      _scope.ServiceProvider.GetRequiredService<KafkaTopicNames>(),
      _scope.ServiceProvider.GetRequiredService<IOptions<KafkaMessagingOptions>>(),
      _scope.ServiceProvider.GetRequiredService<ILogger<KafkaSender>>(),
      new KafkaTransactionContext()
    );

    Func<Task> act = async () => await sut.SendAsync(message);

    await act.Should().ThrowAsync<ProduceException<string, byte[]>>();
  }

  [Fact]
  public async Task SendAsync_OnGenericException_Rethrows()
  {
    var message = new Message
    {
      Id = Guid.NewGuid(),
      CorrelationId = Guid.NewGuid(),
      Body = "Test"
    };

    var mockProducer = new Mock<IProducer<string, byte[]>>();
    mockProducer
      .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Kafka went away"));

    var sut = new KafkaSender(
      mockProducer.Object,
      _scope.ServiceProvider.GetRequiredService<IMessageSerializer>(),
      _scope.ServiceProvider.GetRequiredService<KafkaTopicNames>(),
      _scope.ServiceProvider.GetRequiredService<IOptions<KafkaMessagingOptions>>(),
      _scope.ServiceProvider.GetRequiredService<ILogger<KafkaSender>>(),
      new KafkaTransactionContext()
    );

    Func<Task> act = async () => await sut.SendAsync(message);

    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("Kafka went away");
  }

  private IConsumer<string, byte[]> CreateTestConsumer()
  {
    var config = new ConsumerConfig
    {
      BootstrapServers = _fixture.BootstrapServers,
      GroupId = $"test-group-{Guid.NewGuid():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false
    };

    return new ConsumerBuilder<string, byte[]>(config).Build();
  }

  public async ValueTask DisposeAsync()
  {
    if (_sut is IAsyncDisposable asyncDisposable)
      await asyncDisposable.DisposeAsync();

    _scope.Dispose(); // 🔥 FIX: ensures DI graph cleanup
    }

}