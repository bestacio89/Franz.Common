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
    private readonly Mock<IOptions<MessagingOptions>> _mockOptions;
    private readonly KafkaConsumerFactory _factory;

    public KafkaConsumerFactoryTests()
    {
      _mockOptions = new Mock<IOptions<MessagingOptions>>();
      _mockOptions.Setup(x => x.Value).Returns(new MessagingOptions
      {
        BootStrapServers = "localhost:9092",
        GroupID = "test-group"
      });
      _factory = new KafkaConsumerFactory(_mockOptions.Object);
    }

    [Fact]
    public void Build_ShouldReturnConsumerWithExpectedConfig()
    {
      // Arrange
      var mockConsumer = new Mock<IConsumer<string, object>>();

      // Act
      var consumer = _factory.Build(mockConsumer.Object);

      // Assert
      Assert.Equal("localhost:9092", consumer.Config.BootstrapServers);
      Assert.Equal("test-group", consumer.Config.GroupId);
      Assert.Equal(AutoOffsetReset.Earliest, consumer.Config.AutoOffsetReset); // Default value
    }
  }
}
