#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Senders
{
  [Collection("KafkaSender")]
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
      var options = Options.Create(new KafkaMessagingOptions
      {
        BootStrapServers = _fixture.BootstrapServers
      });

      // Using a real serializer or a mock that mimics the behavior
      var mockSerializer = new Mock<IMessageSerializer>();
      mockSerializer.Setup(x => x.Serialize(It.IsAny<object>()))
                    .Returns((object body) => body.ToString() ?? string.Empty);

      var assemblyAccessor = new AssemblyAccessorWrapper();
      var logger = new NullLogger<KafkaSender>();

      var sender = new KafkaSender(options, mockSerializer.Object, assemblyAccessor, logger);

      var bodyContent = "integration test payload";
      var message = new Message(bodyContent)
      {
        MessageType = "TestMessageType"
      };
      // Add custom header to test the loop in KafkaSender
      message.Headers.Add("Custom-Header", new[] { "CustomValue" });

      // Act
      await sender.SendAsync(message);

      // Assert by consuming directly from Kafka
      var consumerConfig = new ConsumerConfig
      {
        BootstrapServers = _fixture.BootstrapServers,
        GroupId = $"test-group-{Guid.NewGuid()}",
        AutoOffsetReset = AutoOffsetReset.Earliest
      };

      using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
      var topic = TopicNamer.GetTopicName(assemblyAccessor.GetEntryAssembly());
      consumer.Subscribe(topic);

      var consumeResult = consumer.Consume(TimeSpan.FromSeconds(15));

      // Basic Validation
      Assert.NotNull(consumeResult);
      Assert.Equal(message.CorrelationId.ToString(), consumeResult.Message.Key);
      Assert.Equal(bodyContent, consumeResult.Message.Value);

      // Header Validation - Aligning with KafkaSender mapping
      var messageIdHeader = consumeResult.Message.Headers.GetLastBytes("X-Message-ID");
      Assert.NotNull(messageIdHeader);
      Assert.Equal(message.Id.ToString(), Encoding.UTF8.GetString(messageIdHeader));

      var customHeader = consumeResult.Message.Headers.GetLastBytes("Custom-Header");
      Assert.NotNull(customHeader);
      Assert.Equal("CustomValue", Encoding.UTF8.GetString(customHeader));

      await sender.DisposeAsync();
    }
  }
}