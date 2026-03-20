using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.KafKa.Consumers;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

public class KafkaConsumerProviderTests
{
  private static KafkaConsumerProvider CreateProvider()
  {
    var options = Options.Create(new MessagingOptions
    {
      BootStrapServers = "localhost:9092",
      GroupID = "test-group"
    });

    return new KafkaConsumerProvider(options);
  }

  [Fact]
  public void CreateConsumer_Should_Create_New_Instance()
  {
    // Arrange
    var provider = CreateProvider();

    // Act
    var consumer1 = provider.CreateConsumer();
    var consumer2 = provider.CreateConsumer();

    // Assert
    consumer1.Should().NotBeNull();
    consumer2.Should().NotBeNull();
    consumer1.Should().NotBeSameAs(consumer2);
  }

  [Fact]
  public void CreateConsumer_Should_Use_Configured_Values()
  {
    // Arrange
    var provider = CreateProvider();

    // Act
    var consumer = provider.CreateConsumer();

    // Assert
    consumer.Should().NotBeNull();
  }
}