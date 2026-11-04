#nullable enable
using Franz.Common.Messaging.Kafka.Configuration;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka.Validation;

/// <summary>
/// Validates Kafka transport security and consumer retry timing before a Kafka client is constructed.
/// </summary>
public sealed class KafkaMessagingOptionsValidator : IValidateOptions<KafkaMessagingOptions>
{
  public ValidateOptionsResult Validate(string? name, KafkaMessagingOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    var failures = new List<string>();
    var security = options.Security;

    if (!Enum.IsDefined(security.SecurityProtocol))
      failures.Add("SecurityProtocol must be a defined KafkaSecurityProtocol value.");

    if (security.SaslMechanism.HasValue && !Enum.IsDefined(security.SaslMechanism.Value))
      failures.Add("SaslMechanism must be a defined KafkaSaslMechanism value.");

    var usesSasl = security.SecurityProtocol is KafkaSecurityProtocol.SaslPlaintext or KafkaSecurityProtocol.SaslSsl;
    var usesTls = security.SecurityProtocol is KafkaSecurityProtocol.Ssl or KafkaSecurityProtocol.SaslSsl;

    if (security.SecurityProtocol is KafkaSecurityProtocol.Plaintext or KafkaSecurityProtocol.SaslPlaintext)
    {
      if (!security.AllowInsecureTransport)
        failures.Add("Unencrypted Kafka transport requires Security:AllowInsecureTransport=true.");
    }

    if (usesSasl && !security.SaslMechanism.HasValue)
      failures.Add("SaslMechanism is required when SecurityProtocol uses SASL.");

    if (!usesSasl && security.SaslMechanism.HasValue)
      failures.Add("SaslMechanism must not be configured unless SecurityProtocol uses SASL.");

    if (usesSasl && security.SaslMechanism is KafkaSaslMechanism.Plain
        or KafkaSaslMechanism.ScramSha256
        or KafkaSaslMechanism.ScramSha512)
    {
      if (string.IsNullOrWhiteSpace(security.SaslUsername))
        failures.Add("SaslUsername is required for PLAIN and SCRAM mechanisms.");

      if (string.IsNullOrWhiteSpace(security.SaslPassword))
        failures.Add("SaslPassword is required for PLAIN and SCRAM mechanisms.");
    }

    if (!usesSasl && (!string.IsNullOrWhiteSpace(security.SaslUsername)
        || !string.IsNullOrWhiteSpace(security.SaslPassword)))
      failures.Add("SASL credentials must not be configured when SecurityProtocol does not use SASL.");

    if (!usesTls && (!string.IsNullOrWhiteSpace(security.SslCaLocation)
        || !string.IsNullOrWhiteSpace(security.SslCertificateLocation)
        || !string.IsNullOrWhiteSpace(security.SslKeyLocation)))
      failures.Add("SSL certificate settings require an SSL SecurityProtocol.");

    if (string.IsNullOrWhiteSpace(security.SslCertificateLocation)
        != string.IsNullOrWhiteSpace(security.SslKeyLocation))
      failures.Add("SslCertificateLocation and SslKeyLocation must be configured together.");

    if (options.Consumer is null)
      failures.Add("Consumer options must be configured.");

    if (options.Failure is null)
      failures.Add("Failure options must be configured.");

    if (options.Topic is null)
      failures.Add("Topic options must be configured.");
    else
    {
      if (options.Topic.NumPartitions < 1)
        failures.Add("Topic:NumPartitions must be greater than zero.");
      if (options.Topic.ReplicationFactor < 1)
        failures.Add("Topic:ReplicationFactor must be greater than zero.");
      if (options.Topic.CreationTimeoutMs < 100 || options.Topic.CreationTimeoutMs > 300000)
        failures.Add("Topic:CreationTimeoutMs must be between 100 and 300000 milliseconds.");
      if (options.Topic.Configurations is null)
        failures.Add("Topic:Configurations cannot be null.");
      else if (options.Topic.Configurations.Any(pair =>
          string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null))
        failures.Add("Topic:Configurations must contain non-empty keys and non-null values.");
    }

    if (options.Consumer is null || options.Failure is null)
      return ValidateOptionsResult.Fail(failures);

    if (options.Consumer.MaxPollIntervalMs <= 0)
      failures.Add("Consumer:MaxPollIntervalMs must be greater than zero.");

    if (!Enum.IsDefined(options.ProvisioningMode))
      failures.Add("ProvisioningMode must be a defined value.");
    if (options.OrderingStrategy.HasValue && !Enum.IsDefined(options.OrderingStrategy.Value))
      failures.Add("OrderingStrategy must be a defined MessageOrderingStrategy value.");

    var maxRetryAttempts = options.FailurePolicy?.MaxRetryAttempts ?? options.Failure.MaxRetryAttempts;
    var retryBackoffMilliseconds = options.FailurePolicy?.RetryBackoff.TotalMilliseconds
        ?? options.Failure.RetryBackoffMs;
    var handlerTimeoutMilliseconds = options.FailurePolicy?.HandlerTimeout?.TotalMilliseconds
        ?? options.Failure.HandlerTimeoutMs;

    if (maxRetryAttempts < 0)
      failures.Add("Failure:MaxRetryAttempts cannot be negative.");
    if (retryBackoffMilliseconds < 0)
      failures.Add("Failure:RetryBackoffMs cannot be negative.");
    if (handlerTimeoutMilliseconds < 0)
      failures.Add("Failure:HandlerTimeoutMs cannot be negative.");

    // A single synchronous Consume/handler loop must finish its configured retry
    // and cooperative timeout budget before Kafka revokes the consumer's partitions.
    if (options.Consumer.MaxPollIntervalMs > 0 &&
        maxRetryAttempts >= 0 &&
        retryBackoffMilliseconds >= 0 &&
        handlerTimeoutMilliseconds >= 0)
    {
      var retryBudgetMilliseconds = maxRetryAttempts * retryBackoffMilliseconds
          + (maxRetryAttempts + 1) * handlerTimeoutMilliseconds;

      if (retryBudgetMilliseconds >= options.Consumer.MaxPollIntervalMs)
        failures.Add($"Kafka handler and retry budget ({retryBudgetMilliseconds} ms) must be less than Consumer:MaxPollIntervalMs ({options.Consumer.MaxPollIntervalMs} ms).");
    }

    return failures.Count == 0
        ? ValidateOptionsResult.Success
        : ValidateOptionsResult.Fail(failures);
  }
}
