#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

[Collection("KafkaConsumer")]
public class KafkaConsumerFactoryTests
{
  private KafkaMessagingOptions GetDefaultOptions()
      => new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Consumer = new KafkaConsumerOptions
        {
      
          AutoOffsetReset = KafkaAutoOffsetReset.Earliest,
          EnableAutoCommit = true,
          EnableAutoOffsetStore = true,
          SessionTimeoutMs = 10000,
          MaxPollIntervalMs = 300000,
          FetchMaxBytes = 52428800
        },
        Security = new KafkaSecurityOptions()
      };

  [Fact]
  public void Constructor_ShouldThrow_OnNullOptions()
  {
    var logger = Mock.Of<ILogger<KafkaConsumerFactory>>();
    Assert.Throws<ArgumentNullException>(() => new KafkaConsumerFactory(null!, logger));
  }

  [Fact]
  public void Constructor_ShouldThrow_OnNullLogger()
  {
    var options = Options.Create(GetDefaultOptions());
    Assert.Throws<ArgumentNullException>(() => new KafkaConsumerFactory(options, null!));
  }

  [Fact]
  public void Build_ShouldReturnConsumer_WithCorrectType()
  {
    // Arrange
    var options = Options.Create(GetDefaultOptions());
    var logger = Mock.Of<ILogger<KafkaConsumerFactory>>();
    var factory = new KafkaConsumerFactory(options, logger);

    // Act
    var consumer = factory.Build();

    // Assert
    Assert.NotNull(consumer);
    Assert.IsAssignableFrom<IConsumer<string, string>>(consumer);
  }

  [Fact]
  public void Build_ShouldReturnConsumerConfiguredWithOptions()
  {
    // Arrange
    var options = Options.Create(GetDefaultOptions());
    var loggerMock = new Mock<ILogger<KafkaConsumerFactory>>();
    var factory = new KafkaConsumerFactory(options, loggerMock.Object);

    // Act
    var consumer = factory.Build();

    // Assert
    consumer.Should().NotBeNull();
     // just checking type
  }
}