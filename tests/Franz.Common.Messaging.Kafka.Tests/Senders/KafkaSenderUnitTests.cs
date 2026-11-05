#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Exceptions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Senders;

[Collection("KafkaSender")]
public sealed class KafkaSenderUnitTests
{
  private static (KafkaSender Sender, Mock<IProducer<string, byte[]>> Producer) BuildSender(
      MessageOrderingStrategy strategy = MessageOrderingStrategy.MessageId,
      string? topicName = null,
      Action<Mock<IMessageSerializer>>? configureSerializer = null)
  {
    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Name).Returns("Company.Test.Api");
    mockAssembly.Setup(a => a.Assembly).Returns(typeof(KafkaSender).Assembly);

    var mockAccessor = new Mock<IAssemblyAccessor>();
    mockAccessor.Setup(a => a.GetEntryAssembly()).Returns(mockAssembly.Object);

    var mockProducer = new Mock<IProducer<string, byte[]>>();
    mockProducer.Setup(p => p.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<Confluent.Kafka.Message<string, byte[]>>(),
        It.IsAny<CancellationToken>()))
      .ReturnsAsync((string topic, Confluent.Kafka.Message<string, byte[]> message, CancellationToken _) =>
          new DeliveryResult<string, byte[]>
          {
            TopicPartitionOffset = new TopicPartitionOffset(
                new TopicPartition(topic, new Partition(0)), new Offset(1))
          });

    var mockSerializer = new Mock<IMessageSerializer>();
    mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
    configureSerializer?.Invoke(mockSerializer);

    var options = Options.Create(new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = "test-group",
      OrderingStrategy = strategy
    });

    var topicNames = new KafkaTopicNames(mockAccessor.Object, options);

    return (
      new KafkaSender(mockProducer.Object, mockSerializer.Object, topicNames,
          options, NullLogger<KafkaSender>.Instance, new Franz.Common.Messaging.Kafka.Transactions.KafkaTransactionContext(), topicName),
      mockProducer);
  }

  [Fact]
  public async Task SendAsync_ShouldUseExplicitEventTopic_WhenConfigured()
  {
    var (sender, producer) = BuildSender(topicName: "hero-created-in");
    string? actualTopic = null;
    producer.Setup(value => value.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<Confluent.Kafka.Message<string, byte[]>>(),
        It.IsAny<CancellationToken>()))
      .Callback<string, Confluent.Kafka.Message<string, byte[]>, CancellationToken>(
          (topic, _, _) => actualTopic = topic)
      .ReturnsAsync(new DeliveryResult<string, byte[]>
      {
        TopicPartitionOffset = new TopicPartitionOffset(
            new TopicPartition("hero-created-in", new Partition(0)), new Offset(1))
      });

    await sender.SendAsync(new Message());

    actualTopic.Should().Be("hero-created-in");
  }

  [Fact]
  public async Task SendAsync_WhenSerializerReportsInvalidPayload_ShouldClassifyFailureAsPermanent()
  {
    var (sender, producer) = BuildSender(configureSerializer: serializer =>
        serializer.Setup(value => value.Serialize(It.IsAny<object>()))
            .Throws(new JsonException("Invalid payload shape.")));

    var act = async () => await sender.SendAsync(new Message());

    await act.Should().ThrowAsync<PermanentMessagingException>()
        .WithMessage("*could not be serialized*");
    producer.Verify(value => value.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<Confluent.Kafka.Message<string, byte[]>>(),
        It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SendAsync_WhenSerializerFailsTransiently_ShouldPreserveRetryableFailure()
  {
    var (sender, producer) = BuildSender(configureSerializer: serializer =>
        serializer.Setup(value => value.Serialize(It.IsAny<object>()))
            .Throws(new IOException("Temporary schema service outage.")));

    var act = () => sender.SendAsync(new Message());

    await act.Should().ThrowAsync<IOException>()
        .WithMessage("*Temporary schema service outage*");
    producer.Verify(value => value.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<Confluent.Kafka.Message<string, byte[]>>(),
        It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SendAsync_ShouldThrow_WhenMessageIsNull()
  {
    var (sender, _) = BuildSender();
    await using (sender)
    {
      Func<Task> act = async () => await sender.SendAsync(null!);
      await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }
  }

  [Fact]
  public async Task SendAsync_ShouldThrow_WhenDisposed()
  {
    var (sender, _) = BuildSender();
    await sender.DisposeAsync();

    Func<Task> act = async () => await sender.SendAsync(new Message());
    await act.Should().ThrowAsync<ObjectDisposedException>();
  }

  [Fact]
  public async Task SendAsync_ShouldUseMessageIdByDefault()
  {
    var (sender, producer) = BuildSender();
    var message = new Message { CorrelationId = Guid.NewGuid() };
    Confluent.Kafka.Message<string, byte[]>? produced = null;
    producer.Setup(p => p.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<Confluent.Kafka.Message<string, byte[]>>(),
        It.IsAny<CancellationToken>()))
      .Callback<string, Confluent.Kafka.Message<string, byte[]>, CancellationToken>(
          (_, value, _) => produced = value)
      .ReturnsAsync(new DeliveryResult<string, byte[]>
      {
        TopicPartitionOffset = new TopicPartitionOffset(
            new TopicPartition("Company.Test.Api", new Partition(0)), new Offset(1))
      });

    await sender.SendAsync(message);
    produced!.Key.Should().Be(message.Id.ToString());
  }

  [Fact]
  public async Task SendAsync_ShouldUseAggregateId_WhenConfigured()
  {
    var (sender, producer) = BuildSender(MessageOrderingStrategy.AggregateId);
    var aggregateId = Guid.NewGuid();
    var message = new Message { AggregateId = aggregateId };
    Confluent.Kafka.Message<string, byte[]>? produced = null;
    producer.Setup(p => p.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<Confluent.Kafka.Message<string, byte[]>>(),
        It.IsAny<CancellationToken>()))
      .Callback<string, Confluent.Kafka.Message<string, byte[]>, CancellationToken>(
          (_, value, _) => produced = value)
      .ReturnsAsync(new DeliveryResult<string, byte[]>
      {
        TopicPartitionOffset = new TopicPartitionOffset(
            new TopicPartition("Company.Test.Api", new Partition(0)), new Offset(1))
      });

    await sender.SendAsync(message);
    produced!.Key.Should().Be(aggregateId.ToString());
  }

  [Fact]
  public async Task SendAsync_ShouldFail_WhenAggregateIdIsMissingForAggregateStrategy()
  {
    var (sender, producer) = BuildSender(MessageOrderingStrategy.AggregateId);
    Func<Task> act = async () => await sender.SendAsync(new Message());

    await act.Should().ThrowAsync<PermanentMessagingException>()
      .WithMessage("*no AggregateId*");
    producer.Verify(p => p.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<Confluent.Kafka.Message<string, byte[]>>(),
        It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SendAsync_ShouldFail_WhenCorrelationIdIsMissingForCorrelationStrategy()
  {
    var (sender, producer) = BuildSender(MessageOrderingStrategy.CorrelationId);
    var message = new Message { CorrelationId = null };
    Func<Task> act = async () => await sender.SendAsync(message);

    await act.Should().ThrowAsync<PermanentMessagingException>()
      .WithMessage("*no CorrelationId*");
    producer.Verify(p => p.ProduceAsync(
        It.IsAny<string>(),
        It.IsAny<Confluent.Kafka.Message<string, byte[]>>(),
        It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public void Dispose_ShouldBeIdempotent()
  {
    var (sender, _) = BuildSender();
    sender.Dispose();
    sender.Dispose();
    true.Should().BeTrue();
  }

  [Fact]
  public async Task DisposeAsync_ShouldBeIdempotent()
  {
    var (sender, _) = BuildSender();
    await sender.DisposeAsync();
    await sender.DisposeAsync();
    true.Should().BeTrue();
  }
}