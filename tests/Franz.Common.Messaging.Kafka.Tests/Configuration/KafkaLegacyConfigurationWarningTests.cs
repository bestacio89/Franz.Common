#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Configuration;

public sealed class KafkaLegacyConfigurationWarningTests
{
  [Fact]
  public void Validate_WhenLegacyAndCanonicalFailurePoliciesDisagree_ShouldLogWarning()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:Kafka:Failure:MaxRetryAttempts"] = "3",
          ["Messaging:Kafka:Failure:RetryBackoffMs"] = "500",
          ["Messaging:Kafka:Failure:EnableDeadLetter"] = "true",
          ["Messaging:Kafka:FailurePolicy:MaxRetryAttempts"] = "5",
          ["Messaging:Kafka:FailurePolicy:RetryBackoff"] = "00:00:01",
          ["Messaging:Kafka:FailurePolicy:DeadLetterOnExhaustion"] = "false"
        })
        .Build();
    var logger = new Mock<ILogger<KafkaMessagingOptionsValidator>>();
    var validator = new KafkaMessagingOptionsValidator(logger.Object, configuration);

    var options = new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = "legacy-config-test",
      Security = new KafkaSecurityOptions { SecurityProtocol = KafkaSecurityProtocol.Ssl }
    };

    validator.Validate(Microsoft.Extensions.Options.Options.DefaultName, options);

    logger.Verify(
        value => value.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) =>
                state.ToString()!.Contains("conflicting values", StringComparison.OrdinalIgnoreCase)),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  [Fact]
  public void Validate_WhenLegacyAndCanonicalFailurePoliciesAgree_ShouldNotLogConflictWarning()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:Kafka:Failure:MaxRetryAttempts"] = "3",
          ["Messaging:Kafka:Failure:RetryBackoffMs"] = "500",
          ["Messaging:Kafka:Failure:EnableDeadLetter"] = "true",
          ["Messaging:Kafka:FailurePolicy:MaxRetryAttempts"] = "3",
          ["Messaging:Kafka:FailurePolicy:RetryBackoff"] = "00:00:00.500",
          ["Messaging:Kafka:FailurePolicy:DeadLetterOnExhaustion"] = "true"
        })
        .Build();
    var logger = new Mock<ILogger<KafkaMessagingOptionsValidator>>();
    var validator = new KafkaMessagingOptionsValidator(logger.Object, configuration);

    var options = new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = "legacy-config-test",
      Security = new KafkaSecurityOptions { SecurityProtocol = KafkaSecurityProtocol.Ssl }
    };

    validator.Validate(Microsoft.Extensions.Options.Options.DefaultName, options);

    logger.Verify(
        value => value.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) =>
                state.ToString()!.Contains("conflicting values", StringComparison.OrdinalIgnoreCase)),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Never);
  }
}
