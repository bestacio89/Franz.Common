#nullable enable
using Franz.Common.Messaging.Kafka.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Franz.Common.Messaging.Configuration;

namespace Franz.Common.Messaging.Kafka.Validation;

/// <summary>
/// Validates Kafka transport security and consumer retry timing before a Kafka client is constructed.
/// </summary>
public sealed class KafkaMessagingOptionsValidator : IValidateOptions<KafkaMessagingOptions>
{
  private readonly ILogger<KafkaMessagingOptionsValidator>? _logger;

  public KafkaMessagingOptionsValidator(ILogger<KafkaMessagingOptionsValidator>? logger = null)
      => _logger = logger;

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

    if (options.Producer is null)
      failures.Add("Producer options must be configured.");

    ValidateTopicOptions("Topic", options.Topic, failures);
    if (options.DeadLetterTopicOptions is not null)
      ValidateTopicOptions("DeadLetterTopicOptions", options.DeadLetterTopicOptions, failures);

    if (options.EventTopicOptions is null)
      failures.Add("EventTopicOptions cannot be null.");
    else
    {
      foreach (var (topicName, topicOptions) in options.EventTopicOptions)
      {
        if (string.IsNullOrWhiteSpace(topicName))
          failures.Add("EventTopicOptions keys must be non-empty generated topic names.");
        ValidateTopicOptions($"EventTopicOptions:{topicName}", topicOptions, failures);
      }
    }

    if (options.Consumer is null || options.Producer is null)
      return ValidateOptionsResult.Fail(failures);

    if (options.Producer.DeliveryTimeoutMs <= 0 || options.Producer.DeliveryTimeoutMs > 900000)
      failures.Add("Producer:DeliveryTimeoutMs must be between 1 and 900000 milliseconds.");
    if (options.Producer.TransactionTimeoutMs <= 0 || options.Producer.TransactionTimeoutMs > 900000)
      failures.Add("Producer:TransactionTimeoutMs must be between 1 and 900000 milliseconds.");
    else if (options.Producer.DeliveryTimeoutMs > options.Producer.TransactionTimeoutMs)
      failures.Add("Producer:DeliveryTimeoutMs must be less than or equal to Producer:TransactionTimeoutMs for transactional producers.");

    if (options.Consumer.MaxPollIntervalMs <= 0)
      failures.Add("Consumer:MaxPollIntervalMs must be greater than zero.");

    if (!Enum.IsDefined(options.ProvisioningMode))
      failures.Add("ProvisioningMode must be a defined value.");
    if (!Enum.IsDefined(options.OrderingStrategy))
      failures.Add("OrderingStrategy must be a defined MessageOrderingStrategy value.");

    var failurePolicy = options.FailurePolicy ?? MessageFailurePolicy.Default;
    var maxRetryAttempts = failurePolicy.MaxRetryAttempts;
    var retryBackoffMilliseconds = failurePolicy.RetryBackoff.TotalMilliseconds;
    var handlerTimeoutMilliseconds = failurePolicy.HandlerTimeout?.TotalMilliseconds ?? 0;

    if (failurePolicy.HandlerTimeout is null)
      _logger?.LogWarning("Kafka handlers have no configured cooperative timeout; retry-budget validation cannot bound handler execution time.");

    if (maxRetryAttempts < 0)
      failures.Add("FailurePolicy:MaxRetryAttempts cannot be negative.");
    if (retryBackoffMilliseconds < 0)
      failures.Add("FailurePolicy:RetryBackoff cannot be negative.");
    if (handlerTimeoutMilliseconds < 0)
      failures.Add("FailurePolicy:HandlerTimeout cannot be negative.");

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
        failures.Add($"Kafka handler timeout and retry backoff budget ({retryBudgetMilliseconds} ms) must be less than Consumer:MaxPollIntervalMs ({options.Consumer.MaxPollIntervalMs} ms).");
    }

    return failures.Count == 0
        ? ValidateOptionsResult.Success
        : ValidateOptionsResult.Fail(failures);
  }
  private static void ValidateTopicOptions(string name, KafkaTopicOptions? topicOptions, ICollection<string> failures)
  {
    if (topicOptions is null)
    {
      failures.Add($"{name} must be configured.");
      return;
    }

    if (topicOptions.NumPartitions < 1)
      failures.Add($"{name}:NumPartitions must be greater than zero.");
    if (topicOptions.ReplicationFactor < 1)
      failures.Add($"{name}:ReplicationFactor must be greater than zero.");
    if (topicOptions.CreationTimeoutMs < 100 || topicOptions.CreationTimeoutMs > 300000)
      failures.Add($"{name}:CreationTimeoutMs must be between 100 and 300000 milliseconds.");
    if (topicOptions.Configurations is null)
      failures.Add($"{name}:Configurations cannot be null.");
    else if (topicOptions.Configurations.Any(pair =>
        string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null))
      failures.Add($"{name}:Configurations must contain non-empty keys and non-null values.");
  }

}
