#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Org.BouncyCastle.Bcpg.Sig;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;

namespace Franz.Common.Messaging.Kafka.Tests.Senders
{
  [Collection("Kafka")]
  public class KafkaSenderIntegrationTests
  {
    private readonly KafkaContainerFixture _fixture;

    public KafkaSenderIntegrationTests(KafkaContainerFixture fixture)
    {
      _fixture = fixture;
    }

    [Fact]
    public async Task SendAsync_ShouldProduceMessageToKafka()
    {
      // Arrange
      var options = Microsoft.Extensions.Options.Options.Create(new MessagingOptions
      {
        BootStrapServers = _fixture.BootstrapServers
      });

      var serializer = new JsonMessageSerializer(); // your real implementation
      var assemblyAccessor = new AssemblyAccessorWrapper();
      var logger = new NullLogger<KafkaSender>();

      var sender = new KafkaSender(options, serializer, assemblyAccessor, logger);

      var message = new Message("integration test payload");

      // Act
      await sender.SendAsync(message);

      // Assert by consuming directly from Kafka
      var consumerConfig = new Confluent.Kafka.ConsumerConfig
      {
        BootstrapServers = _fixture.BootstrapServers,
        GroupId = "test-group",
        AutoOffsetReset = AutoOffsetReset.Earliest
      };

      using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
      consumer.Subscribe(sender.GetType().Assembly.GetName().Name); // same topic naming logic

      var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

      Assert.NotNull(consumeResult);
      Assert.Equal(message.CorrelationId.ToString(), consumeResult.Message.Key);
      Assert.Equal(serializer.Serialize(message.Body!), consumeResult.Message.Value);

      // Check headers
      var correlationHeader = consumeResult.Message.Headers.GetLastBytes("X-Correlation-ID");
      Assert.NotNull(correlationHeader);
      Assert.Equal(message.CorrelationId.ToString(), Encoding.UTF8.GetString(correlationHeader));
    }
  }

  internal static class KafkaExtensions
  {
    public static byte[] GetLastBytesSafe(this Confluent.Kafka.Headers headers, string key)
    {
      // Returns empty array if key does not exist
      return headers.GetLastBytes(key) ?? Array.Empty<byte>();
    }
  }
}