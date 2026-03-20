using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.KafKa.Consumers;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

public class KafkaConsumerGroupTests
{
  private static KafkaConsumerGroup CreateGroup()
  {
    var options = Options.Create(new MessagingOptions
    {
      BootStrapServers = "localhost:9092",
      GroupID = "group-1"
    });

    return new KafkaConsumerGroup(options);
  }

  [Fact]
  public void CreateConsumer_Should_Return_Same_Instance()
  {
    // Arrange
    var group = CreateGroup();

    // Act
    var c1 = group.CreateConsumer();
    var c2 = group.CreateConsumer();

    // Assert
    c1.Should().BeSameAs(c2);
  }

  [Fact]
  public void Subscribe_Should_Not_Throw()
  {
    // Arrange
    var group = CreateGroup();

    // Act
    Action act = () => group.Subscribe("test-topic");

    // Assert
    act.Should().NotThrow();
  }

  [Fact]
  public void Unsubscribe_Should_Not_Throw()
  {
    // Arrange
    var group = CreateGroup();
    group.Subscribe("test-topic");

    // Act
    Action act = group.Unsubscribe;

    // Assert
    act.Should().NotThrow();
  }

  [Fact]
  public void Dispose_Should_Dispose_Consumer()
  {
    // Arrange
    var group = CreateGroup();
    var consumer = group.CreateConsumer();

    // Act
    group.Dispose();

    // Assert
    Action act = () => consumer.Subscribe("another-topic");
    act.Should().Throw<ObjectDisposedException>();
  }
}