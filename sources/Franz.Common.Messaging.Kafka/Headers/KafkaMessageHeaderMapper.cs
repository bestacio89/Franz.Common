#nullable enable

using System.Text;
using Confluent.Kafka;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Kafka.Headers;

/// <summary>
/// Reconciles Kafka record headers with the serialized Franz message envelope.
/// The envelope and transport projection must agree when both provide a value.
/// </summary>
public static class KafkaMessageHeaderMapper
{
  private const string MessageIdHeader = "X-Message-ID";
  private static readonly UTF8Encoding StrictUtf8 = new(false, true);

  /// <summary>
  /// Restores metadata that may have been supplied as native Kafka headers.
  /// Repeated values are retained for multi-valued headers; conflicting projections
  /// are rejected instead of silently choosing one representation.
  /// </summary>
  public static void Restore(Message message, Headers? transportHeaders)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (transportHeaders is null || transportHeaders.Count == 0)
      return;

    var decodedHeaders = transportHeaders
        .Select(header => new KeyValuePair<string, string>(
            header.Key,
            Decode(header)))
        .ToArray();

    RestoreMessageIdentity(message, decodedHeaders);

    foreach (var group in decodedHeaders
        .Where(header => !string.Equals(header.Key, MessageIdHeader, StringComparison.OrdinalIgnoreCase))
        .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase))
    {
      var values = group.Select(header => header.Value).ToArray();
      if (values.Length == 0)
        continue;

      if (IsSingleValueContractHeader(group.Key) && values.Length != 1)
        throw new InvalidDataException(
            $"Kafka header '{group.Key}' must contain exactly one value.");

      if (string.Equals(group.Key, Message.CorrelationHeader, StringComparison.OrdinalIgnoreCase))
      {
        RestoreCorrelation(message, values[0]);
        continue;
      }

      var existing = FindHeader(message, group.Key);
      if (existing is not null)
      {
        if (!existing.SequenceEqual(values, StringComparer.Ordinal))
          throw new InvalidDataException(
              $"Kafka header '{group.Key}' conflicts with the serialized Franz message envelope.");

        continue;
      }

      message.Headers[group.Key] = values;
    }
  }

  private static void RestoreMessageIdentity(
      Message message,
      IReadOnlyCollection<KeyValuePair<string, string>> headers)
  {
    var identities = headers
        .Where(header => string.Equals(header.Key, MessageIdHeader, StringComparison.OrdinalIgnoreCase))
        .Select(header => header.Value)
        .ToArray();

    if (identities.Length == 0)
      return;

    if (identities.Length != 1 ||
        !Guid.TryParse(identities[0], out var transportId) ||
        transportId == Guid.Empty)
      throw new InvalidDataException(
          "Kafka must provide exactly one valid non-empty message identity.");

    if (message.Id != Guid.Empty && message.Id != transportId)
      throw new InvalidDataException(
          "Kafka message identity conflicts with the serialized Franz message envelope.");

    message.Id = transportId;
  }

  private static void RestoreCorrelation(Message message, string value)
  {
    if (!Guid.TryParse(value, out var transportCorrelationId) || transportCorrelationId == Guid.Empty)
      throw new InvalidDataException("Kafka correlation metadata must contain one valid non-empty GUID.");

    if (message.CorrelationId.HasValue && message.CorrelationId.Value != transportCorrelationId)
      throw new InvalidDataException(
          "Kafka correlation metadata conflicts with the serialized Franz message envelope.");

    message.CorrelationId = transportCorrelationId;
  }

  private static string Decode(Header header)
  {
    if (header.GetValueBytes() is not { } bytes)
      throw new InvalidDataException($"Kafka header '{header.Key}' has a null value.");

    try
    {
      return StrictUtf8.GetString(bytes);
    }
    catch (DecoderFallbackException exception)
    {
      throw new InvalidDataException(
          $"Kafka header '{header.Key}' does not contain valid UTF-8 data.",
          exception);
    }
  }

  private static string[]? FindHeader(Message message, string key)
  {
    foreach (var header in message.Headers)
    {
      if (string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
        return header.Value;
    }

    return null;
  }

  private static bool IsSingleValueContractHeader(string key)
      => string.Equals(key, Message.CorrelationHeader, StringComparison.OrdinalIgnoreCase)
          || string.Equals(key, Message.CausationHeader, StringComparison.OrdinalIgnoreCase)
          || string.Equals(key, "TenantId", StringComparison.OrdinalIgnoreCase)
          || string.Equals(key, "DomainId", StringComparison.OrdinalIgnoreCase);
}
