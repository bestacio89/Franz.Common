#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Validation;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Configuration
{
  public class KafkaMessagingOptionsTests
  {
    [Fact]
    public void KafkaMessagingOptions_Should_HaveDefaultValues()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group"
      };

      // Top-level defaults
      options.Security.Should().NotBeNull();
      options.Consumer.Should().NotBeNull();
      options.Producer.Should().NotBeNull();
      options.Failure.Should().NotBeNull();
      options.Observability.Should().NotBeNull();

      // Security defaults
      options.Security.SecurityProtocol.Should().Be(KafkaSecurityProtocol.Plaintext);
      options.Security.SaslMechanism.Should().BeNull();

      // Consumer defaults
      options.Consumer.AutoOffsetReset.Should().Be(KafkaAutoOffsetReset.Earliest);
      options.Consumer.EnableAutoCommit.Should().BeFalse();
      options.Consumer.EnableAutoOffsetStore.Should().BeFalse();
      options.Consumer.SessionTimeoutMs.Should().Be(45000);
      options.Consumer.MaxPollIntervalMs.Should().Be(300000);
      options.Consumer.MaxPollRecords.Should().Be(500);
      options.Consumer.FetchMinBytes.Should().Be(1);
      options.Consumer.FetchMaxBytes.Should().Be(52428800);
      options.Consumer.FetchWaitMaxMs.Should().Be(500);
      options.Consumer.ConcurrencyLevel.Should().Be(1);

      // Producer defaults
      options.Producer.Acks.Should().Be(KafkaAcks.All);
      options.Producer.MessageMaxBytes.Should().Be(1048576);
      options.Producer.LingerMs.Should().Be(5);
      options.Producer.BatchSize.Should().Be(16384);
      options.Producer.CompressionType.Should().Be(KafkaCompressionType.Snappy);
      options.Producer.EnableIdempotence.Should().BeTrue();
      options.Producer.MessageSendMaxRetries.Should().Be(3);
      options.Producer.RetryBackoffMs.Should().Be(100);
      options.Producer.DeliveryTimeoutMs.Should().Be(120000);

      // Failure defaults
      options.Failure.EnableDeadLetter.Should().BeTrue();
      options.Failure.MaxRetryAttempts.Should().Be(3);
      options.Failure.RetryBackoffMs.Should().Be(500);
      options.Failure.UseOriginalMessageKey.Should().BeTrue();

      // Observability defaults
      options.Observability.EnableMetrics.Should().BeTrue();
      options.Observability.EnableTracing.Should().BeTrue();
      options.Observability.EnableLogging.Should().BeTrue();
      options.Observability.LogMessagePayloads.Should().BeFalse();
    }

    [Fact]
    public void KafkaMessagingOptions_ShouldRequire_BootstrapServers_And_GroupId()
    {
      var options = new KafkaMessagingOptions() { BootstrapServers = ""};

      var context = new ValidationContext(options);
      var results = new System.Collections.Generic.List<ValidationResult>();

      var isValid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

      isValid.Should().BeFalse();
      results.Select(r => r.MemberNames.FirstOrDefault())
             .Should().Contain(new[] { "BootstrapServers" });
    }

    [Fact]
    public void KafkaConsumerOptions_ShouldValidate_Ranges()
    {
      var consumer = new KafkaConsumerOptions
      {
        SessionTimeoutMs = 500,      // invalid (<1000)
        MaxPollIntervalMs = 5000000  // invalid (>3000000)
      };

      var context = new ValidationContext(consumer);
      var results = new System.Collections.Generic.List<ValidationResult>();

      var isValid = Validator.TryValidateObject(consumer, context, results, validateAllProperties: true);

      isValid.Should().BeFalse();
      results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(KafkaConsumerOptions.SessionTimeoutMs)));
      results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(KafkaConsumerOptions.MaxPollIntervalMs)));
    }

    [Fact]
    public void KafkaProducerOptions_ShouldValidate_Range_MessageMaxBytes()
    {
      var producer = new KafkaProducerOptions
      {
        MessageMaxBytes = 512 // invalid (<1024)
      };

      var context = new ValidationContext(producer);
      var results = new System.Collections.Generic.List<ValidationResult>();

      var isValid = Validator.TryValidateObject(producer, context, results, validateAllProperties: true);

      isValid.Should().BeFalse();
      results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(KafkaProducerOptions.MessageMaxBytes)));
    }

    [Fact]
    public void KafkaSecurityOptions_ShouldReject_PlaintextWithoutExplicitOptIn()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group"
      };

      var result = new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options);

      result.Failed.Should().BeTrue();
      result.Failures.Should().Contain(message => message.Contains("AllowInsecureTransport"));
    }

    [Theory]
    [InlineData(KafkaSecurityProtocol.SaslPlaintext)]
    [InlineData(KafkaSecurityProtocol.SaslSsl)]
    public void KafkaSecurityOptions_ShouldRequire_SaslMechanism(KafkaSecurityProtocol protocol)
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Security = new KafkaSecurityOptions
        {
          SecurityProtocol = protocol,
          SaslUsername = "user",
          SaslPassword = "secret"
        }
      };

      var result = new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options);

      result.Failed.Should().BeTrue();
      result.Failures.Should().Contain(message => message.Contains("SaslMechanism"));
    }

    [Fact]
    public void KafkaSecurityOptions_ShouldRequireCredentials_ForPlainMechanism()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Security = new KafkaSecurityOptions
        {
          SecurityProtocol = KafkaSecurityProtocol.SaslSsl,
          SaslMechanism = KafkaSaslMechanism.Plain
        }
      };

      var result = new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options);

      result.Failed.Should().BeTrue();
      result.Failures.Should().Contain(message => message.Contains("SaslUsername"));
      result.Failures.Should().Contain(message => message.Contains("SaslPassword"));
    }

    [Fact]
    public void KafkaSecurityOptions_ShouldAccept_ValidSaslSslConfiguration()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Security = new KafkaSecurityOptions
        {
          SecurityProtocol = KafkaSecurityProtocol.SaslSsl,
          SaslMechanism = KafkaSaslMechanism.ScramSha512,
          SaslUsername = "user",
          SaslPassword = "secret"
        }
      };

      new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options).Failed.Should().BeFalse();
    }

    [Fact]
    public void KafkaSecurityOptions_ShouldReject_SslClientCertificateWithoutKey()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Security = new KafkaSecurityOptions
        {
          SecurityProtocol = KafkaSecurityProtocol.Ssl,
          SslCertificateLocation = "/cert/client.pem"
        }
      };

      var result = new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options);

      result.Failed.Should().BeTrue();
      result.Failures.Should().Contain(message => message.Contains("SslKeyLocation"));
    }

    [Fact]
    public void KafkaSecurityOptions_ShouldHandle_NullsAndEnums()
    {
      var security = new KafkaSecurityOptions();

      security.SecurityProtocol.Should().Be(KafkaSecurityProtocol.Plaintext);
      security.SaslMechanism.Should().BeNull();
      security.SaslUsername.Should().BeNull();
      security.SaslPassword.Should().BeNull();
    }

    [Fact]
    public void KafkaFailureOptions_ShouldHave_DefaultValues()
    {
      var failure = new KafkaFailureOptions();

      failure.EnableDeadLetter.Should().BeTrue();
      failure.DeadLetterTopic.Should().BeNull();
      failure.MaxRetryAttempts.Should().Be(3);
      failure.RetryBackoffMs.Should().Be(500);
      failure.UseOriginalMessageKey.Should().BeTrue();
    }

    [Fact]
    public void KafkaObservabilityOptions_ShouldHave_Defaults()
    {
      var obs = new KafkaObservabilityOptions();

      obs.EnableMetrics.Should().BeTrue();
      obs.EnableTracing.Should().BeTrue();
      obs.EnableLogging.Should().BeTrue();
      obs.LogMessagePayloads.Should().BeFalse();
    }

    [Fact]
    public void KafkaFailureOptions_ShouldReject_RetryBackoffBudgetThatExceedsMaxPollInterval()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Security = new KafkaSecurityOptions
        {
          SecurityProtocol = KafkaSecurityProtocol.Ssl
        },
        Consumer = new KafkaConsumerOptions { MaxPollIntervalMs = 1000 },
        Failure = new KafkaFailureOptions { MaxRetryAttempts = 3, RetryBackoffMs = 334 }
      };

      var result = new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options);

      result.Failed.Should().BeTrue();
      result.Failures.Should().Contain(message =>
          message.Contains("retry backoff budget", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void KafkaFailureOptions_ShouldAccept_RetryBackoffBudgetBelowMaxPollInterval()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Security = new KafkaSecurityOptions
        {
          SecurityProtocol = KafkaSecurityProtocol.Ssl
        },
        Consumer = new KafkaConsumerOptions { MaxPollIntervalMs = 1000 },
        Failure = new KafkaFailureOptions { MaxRetryAttempts = 3, RetryBackoffMs = 333 }
      };

      var result = new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options);

      result.Failed.Should().BeFalse();
    }

    [Fact]
    public void KafkaFailureOptions_ShouldAllowZeroRetryBudget()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Security = new KafkaSecurityOptions
        {
          SecurityProtocol = KafkaSecurityProtocol.Ssl
        },
        Consumer = new KafkaConsumerOptions { MaxPollIntervalMs = 1000 },
        Failure = new KafkaFailureOptions { MaxRetryAttempts = 0, RetryBackoffMs = 60000 }
      };

      var result = new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options);

      result.Failed.Should().BeFalse();
    }

    [Fact]
    public void KafkaTopicOptions_ShouldHaveSafeDefaults()
    {
      var topic = new KafkaTopicOptions();
      topic.NumPartitions.Should().Be(1);
      topic.ReplicationFactor.Should().Be(1);
      topic.CreationTimeoutMs.Should().Be(30000);
      topic.Configurations.Should().BeEmpty();
    }

    [Fact]
    public void KafkaTopicOptions_ShouldRejectInvalidProvisioningValues()
    {
      var options = new KafkaMessagingOptions
      {
        BootstrapServers = "localhost:9092",
        GroupId = "test-group",
        Security = new KafkaSecurityOptions { SecurityProtocol = KafkaSecurityProtocol.Ssl },
        Topic = new KafkaTopicOptions
        {
          NumPartitions = 0,
          ReplicationFactor = 0,
          CreationTimeoutMs = 0
        }
      };

      var result = new KafkaMessagingOptionsValidator().Validate(
          Microsoft.Extensions.Options.Options.DefaultName, options);

      result.Failed.Should().BeTrue();
      result.Failures.Should().Contain(message => message.Contains("NumPartitions"));
      result.Failures.Should().Contain(message => message.Contains("ReplicationFactor"));
      result.Failures.Should().Contain(message => message.Contains("CreationTimeoutMs"));
    }
  }
}