#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Senders
{
  [Collection("Kafka")]
  public class MessagingSenderIntegrationTests
  {
    private readonly KafkaContainerFixture _fixture;

    public MessagingSenderIntegrationTests(KafkaContainerFixture fixture)
    {
      _fixture = fixture;
    }

    [Fact]
    public async Task SendAsync_ShouldProduceMessageToKafka()
    {
      // Arrange
      var producerConfig = new ProducerConfig
      {
        BootstrapServers = _fixture.BootstrapServers,
        EnableIdempotence = true
      };
      using var producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

      var assemblyAccessor = new AssemblyAccessorWrapper();
      var logger = new NullLogger<MessagingSender>();

      var sender = new MessagingSender(producer, assemblyAccessor, logger);

      var message = new Message("integration test payload")
      {
        MessageType = "TestMessageType"
      };

      // Act
      await sender.SendAsync(message);

      // Assert by consuming directly from Kafka
      var consumerConfig = new ConsumerConfig
      {
        BootstrapServers = _fixture.BootstrapServers,
        GroupId = "test-group",
        AutoOffsetReset = AutoOffsetReset.Earliest
      };

      using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
      consumer.Subscribe(TopicNamer.GetTopicName(assemblyAccessor.GetEntryAssembly()));

      var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));
      Assert.NotNull(consumeResult);

      // Key & payload
      Assert.Equal(message.CorrelationId.ToString(), consumeResult.Message.Key);
      Assert.Equal(message.Body, Encoding.UTF8.GetString(consumeResult.Message.Value));

      // Headers
      var correlationHeader = consumeResult.Message.Headers.GetLastBytesSafe("correlation-id");
      Assert.NotNull(correlationHeader);
      Assert.Equal(message.CorrelationId.ToString(), Encoding.UTF8.GetString(correlationHeader));

      var messageIdHeader = consumeResult.Message.Headers.GetLastBytesSafe("message-id");
      Assert.Equal(message.Id.ToString(), Encoding.UTF8.GetString(messageIdHeader));

      var messageTypeHeader = consumeResult.Message.Headers.GetLastBytesSafe("message-type");
      Assert.Equal(message.MessageType, Encoding.UTF8.GetString(messageTypeHeader));
    }
  }

  
}