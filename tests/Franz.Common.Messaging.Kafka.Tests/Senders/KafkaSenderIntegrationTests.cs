#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Senders;

[Collection("KafkaIntegration")]
public sealed class KafkaSenderTests : IClassFixture<KafkaContainerFixture>, IAsyncDisposable
{
  private readonly KafkaContainerFixture _fixture;
  private readonly IServiceProvider _serviceProvider;
  private readonly IServiceScope _scope;

  // System Under Test
  private readonly KafkaSender _sut;
  private readonly string _topicName = "integration-test";

  public KafkaSenderTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture;
    _serviceProvider = fixture.BuildServiceProvider();
    _scope = _serviceProvider.CreateScope();

    // AddKafkaMessaging likely maps the interface exclusively. 
    // We scan the registered interfaces to extract the concrete SUT to avoid Missing Service exceptions.
    _sut = _scope.ServiceProvider.GetServices<IMessagingSender>()
        .OfType<KafkaSender>()
        .SingleOrDefault()
        ?? throw new InvalidOperationException("KafkaSender could not be found among registered IMessagingSender instances. Verify AddKafkaMessaging configuration.");
  }

  [Fact]
  [Trait("Category", "Integration")]
  public async Task SendAsync_ValidMessage_DeliversSuccessfullyToKafka()
  {
    // Arrange
    var messageId = Guid.NewGuid();
    var correlationId = Guid.NewGuid();
    var payload = new TestPayload("Integration-Test-Data");

    var message = new Message
    {
      Id = messageId,
      CorrelationId = correlationId,
      Body = payload.Data,
      Headers = new Dictionary<string, string[]>
        {
            { "Custom-Header", new[] { "Value1" } }
        }
    };

    // 🔥 Resolve the EXACT same topic as KafkaSender
    var topic = TopicNamer.GetTopicName(
        _scope.ServiceProvider
            .GetRequiredService<IAssemblyAccessor>()
            .GetEntryAssembly());

    using var consumer = CreateTestConsumer();

    // Subscribe BEFORE producing to avoid race conditions
    consumer.Subscribe(topic);

    // Act
    await _sut.SendAsync(message);

    // Assert
    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

    consumeResult.Should().NotBeNull("Message should be delivered within timeout");

    consumeResult!.Message.Key.Should().Be(correlationId.ToString());

    var receivedPayload = Encoding.UTF8.GetString(consumeResult.Message.Value);
    receivedPayload.Should().Contain("Integration-Test-Data");

    var headers = consumeResult.Message.Headers.ToDictionary(
        h => h.Key,
        h => Encoding.UTF8.GetString(h.GetValueBytes()));

    headers.Should().ContainKey("X-Message-ID");
    headers["X-Message-ID"].Should().Be(messageId.ToString());

    headers.Should().ContainKey("Custom-Header");
    headers["Custom-Header"].Should().Be("Value1");

    consumer.Close();
  }

  [Fact]
  [Trait("Category", "Integration")]
  public async Task SendAsync_NullMessage_ThrowsArgumentNullException()
  {
    // Act
    Func<Task> act = async () => await _sut.SendAsync(null!);

    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>()
        .WithParameterName("message");
  }

  [Fact]
  [Trait("Category", "Integration")]
  public async Task SendAsync_WhenDisposed_ThrowsObjectDisposedException()
  {
    // Arrange
    var message = new Message { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Body = new TestPayload("Test").Data };
    await _sut.DisposeAsync();

    // Act
    Func<Task> act = async () => await _sut.SendAsync(message);

    // Assert
    await act.Should().ThrowAsync<ObjectDisposedException>()
        .WithMessage($"*{nameof(KafkaSender)}*");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task SendAsync_OnProduceException_LogsErrorAndRethrows()
  {
    // Arrange
    var message = new Message { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Body = new TestPayload("Test").Data   };

    var mockProducer = new Mock<IProducer<string, byte[]>>();
    var error = new Error(ErrorCode.Local_MsgTimedOut, "Simulated timeout");
    var produceException = new ProduceException<string, byte[]>(error, null!);

    mockProducer
        .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(produceException);

    var sut = new KafkaSender(
        mockProducer.Object,
        _scope.ServiceProvider.GetRequiredService<IMessageSerializer>(),
        _scope.ServiceProvider.GetRequiredService<IAssemblyAccessor>(),
        _scope.ServiceProvider.GetRequiredService<ILogger<KafkaSender>>()
    );

    // Act
    Func<Task> act = async () => await sut.SendAsync(message);

    // Assert
    await act.Should().ThrowAsync<ProduceException<string, byte[]>>();
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task SendAsync_OnGenericException_LogsErrorAndRethrows()
  {
    // Arrange
    var message = new Message { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Body = new TestPayload("Test").Data };

    var mockProducer = new Mock<IProducer<string, byte[]>>();
    mockProducer
        .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, byte[]>>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("Kafka went away"));

    var sut = new KafkaSender(
        mockProducer.Object,
        _scope.ServiceProvider.GetRequiredService<IMessageSerializer>(),
        _scope.ServiceProvider.GetRequiredService<IAssemblyAccessor>(),
        _scope.ServiceProvider.GetRequiredService<ILogger<KafkaSender>>()
    );

    // Act
    Func<Task> act = async () => await sut.SendAsync(message);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Kafka went away");
  }

  private IConsumer<string, byte[]> CreateTestConsumer()
  {
    var config = new ConsumerConfig
    {
      BootstrapServers = _fixture.BootstrapServers,
      GroupId = $"test-group-{Guid.NewGuid()}",
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false
    };

    return new ConsumerBuilder<string, byte[]>(config).Build();
  }

  public async ValueTask DisposeAsync()
  {
    if (_sut is IAsyncDisposable asyncDisposable)
    {
      await asyncDisposable.DisposeAsync();
    }
    _scope.Dispose();
  }

  private sealed record TestPayload(string Data);
}