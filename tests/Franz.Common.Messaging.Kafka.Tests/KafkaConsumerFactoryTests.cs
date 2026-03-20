using System;
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests
{
  public class KafkaConsumerFactoryTests
  {
    [Fact]
    public void Build_WithValidOptions_ReturnsIConsumer()
    {
      // Arrange
      var optionsMock = new Mock<IOptions<MessagingOptions>>();
      optionsMock.Setup(o => o.Value).Returns(new MessagingOptions
      {
        BootStrapServers = "localhost:9092",
        GroupID = "test-group"
      });

      var factory = new KafkaConsumerFactory(optionsMock.Object);

      // Act
      var consumer = factory.Build();

      // Assert
      Assert.NotNull(consumer);
      Assert.IsAssignableFrom<IConsumer<string, string>>(consumer);

      // Clean up (important to dispose real consumer)
      consumer.Close();
      consumer.Dispose();
    }

    [Fact]
    public void Build_WithoutBootstrapServers_ThrowsArgumentException()
    {
      // Arrange
      var optionsMock = new Mock<IOptions<MessagingOptions>>();
      optionsMock.Setup(o => o.Value).Returns(new MessagingOptions
      {
        BootStrapServers = "", // invalid
        GroupID = "test-group"
      });

      var factory = new KafkaConsumerFactory(optionsMock.Object);

      // Act & Assert
      Assert.Throws<ArgumentException>(() => factory.Build());
    }
  }
}