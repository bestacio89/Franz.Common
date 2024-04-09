using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Options;
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka;

namespace Franz.Common.Messaging.Kafka.Tests
{
  [TestFixture]
  public class KafkaConsumerFactoryTests
  {
    private Mock<IOptions<MessagingOptions>> _mockOptions;
    private KafkaConsumerFactory _factory;

    [SetUp]
    public void SetUp()
    {
      _mockOptions = new Mock<IOptions<MessagingOptions>>();
      _mockOptions.Setup(x => x.Value).Returns(new MessagingOptions
      {
        BootStrapServers = "localhost:9092",
        GroupID = "test-group"
      });
      _factory = new KafkaConsumerFactory(_mockOptions.Object);
    }

    [Test]
    public void Build_ShouldReturnConsumerWithExpectedConfig()
    {
      // Arrange
      var mockConsumer = new Mock<IConsumer<string, object>>();
   
      // Act
      var consumer = new KafkaConsumer(_factory.Build(mockConsumer.Object));

      // Assert
      Assert.AreEqual("localhost:9092", consumer.Config.BootstrapServers);
      Assert.AreEqual("test-group", consumer.Config.GroupId);
      Assert.AreEqual(AutoOffsetReset.Earliest, consumer.Config.AutoOffsetReset);
    }
  }
}
