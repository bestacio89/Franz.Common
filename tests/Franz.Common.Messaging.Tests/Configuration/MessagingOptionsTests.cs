using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ;
using Xunit;

namespace Franz.Common.Messaging.Tests.Configuration;

public class MessagingOptionsTests
{
  [Fact]
  public void BaseMessagingOptions_ShouldHaveDefaultValues()
  {
    var options = new MessagingOptions();

    options.SslEnabled.Should().BeNull();
    options.Port.Should().BeNull();
  }

  [Fact]
  public void BaseMessagingOptions_ShouldSetAndGetProperties()
  {
    var options = new MessagingOptions
    {
      HostName = "localhost",
      Port = 5672,
      SslEnabled = true,
      SslCaLocation = "/ca.pem",
      SslCertificateLocation = "/cert.pem",
      SslKeyLocation = "/key.pem"
    };

    options.HostName.Should().Be("localhost");
    options.Port.Should().Be(5672);
    options.SslEnabled.Should().BeTrue();
    options.SslCaLocation.Should().Be("/ca.pem");
    options.SslCertificateLocation.Should().Be("/cert.pem");
    options.SslKeyLocation.Should().Be("/key.pem");
  }

  [Fact]
  public void RabbitMQMessagingOptions_ShouldSetAndGetRabbitSpecificProperties()
  {
    var options = new RabbitMQMessagingOptions
    {
      ExchangeName = "orders-exchange",
      QueueName = "orders-queue",
      DeadLetterQueueName = "orders-dlq",
      DeadLetterExchangeName = "orders-dlx",
      DefaultRoutingKey = "orders.default",
      RequestedHeartbeatSeconds = 45
    };

    options.ExchangeName.Should().Be("orders-exchange");
    options.QueueName.Should().Be("orders-queue");
    options.DeadLetterQueueName.Should().Be("orders-dlq");
    options.DeadLetterExchangeName.Should().Be("orders-dlx");
    options.DefaultRoutingKey.Should().Be("orders.default");
    options.RequestedHeartbeatSeconds.Should().Be(45);
  }

  [Fact]
  public void KafkaMessagingOptions_ShouldSetAndGetKafkaSpecificProperties()
  {
    var options = new KafkaMessagingOptions
    {
      BootStrapServers = "kafka:9092",
      GroupID = "kafka-group",
      TopicName = "orders-topic",
      DeadLetterTopicName = "orders-dlt",
      Partitions = 3,
      ReplicationFactor = 2
    };

    options.BootStrapServers.Should().Be("kafka:9092");
    options.GroupID.Should().Be("kafka-group");
    options.TopicName.Should().Be("orders-topic");
    options.DeadLetterTopicName.Should().Be("orders-dlt");
    options.Partitions.Should().Be(3);
    options.ReplicationFactor.Should().Be(2);
  }
}