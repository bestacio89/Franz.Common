#nullable enable
using Franz.Common.Messaging.Kafka.Configuration;
using Microsoft.Extensions.Options;

namespace Franz.Common.Messaging.Kafka.Validation;

/// <summary>
/// Validates Kafka transport security before any Kafka client is constructed.
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

    if (!Enum.IsDefined(security.SaslMechanism) && security.SaslMechanism.HasValue)
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

    return failures.Count == 0
        ? ValidateOptionsResult.Success
        : ValidateOptionsResult.Fail(failures);
  }
}
