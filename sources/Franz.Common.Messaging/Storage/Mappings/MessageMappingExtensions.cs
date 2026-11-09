#nullable enable

using System.Globalization;
using System.Text.Json;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;

namespace Franz.Common.Messaging.Storage;

public static class MessageMappingExtensions
{
  private const string StoredMessageKindProperty = "$franz.storage.message-kind";
  private const string StoredAggregateIdProperty = "$franz.storage.aggregate-id";
  private const string StoredOccurredOnProperty = "$franz.storage.occurred-on";

  public static StoredMessage ToStored(this Message message)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(message.Body);

    var headers = message.Headers
        .Where(kv => !StringValues.IsNullOrEmpty(kv.Value) &&
                     kv.Value.Any(value => !string.IsNullOrWhiteSpace(value)))
        .ToDictionary(
            kv => kv.Key,
            kv => kv.Value
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);

    var properties = new Dictionary<string, object>(
        message.Properties,
        StringComparer.OrdinalIgnoreCase)
    {
      [StoredMessageKindProperty] = message.Kind.ToString(),
      [StoredOccurredOnProperty] = message.OccurredOn.ToString("O", CultureInfo.InvariantCulture)
    };

    if (message.AggregateId.HasValue)
      properties[StoredAggregateIdProperty] = message.AggregateId.Value.ToString("D");
    else
      properties.Remove(StoredAggregateIdProperty);

    return new StoredMessage
    {
      Id = message.Id,
      Body = message.Body,
      CorrelationId = message.CorrelationId,
      MessageType = message.MessageType,
      Headers = headers,
      Properties = properties,
      CreatedOn = DateTime.UtcNow
    };
  }

  public static Message ToMessage(this StoredMessage stored)
  {
    ArgumentNullException.ThrowIfNull(stored);

    var properties = new Dictionary<string, object>(
        stored.Properties ?? new Dictionary<string, object>(),
        StringComparer.OrdinalIgnoreCase);

    var kind = TryGetStoredString(properties, StoredMessageKindProperty, out var kindValue) &&
               Enum.TryParse(kindValue, ignoreCase: true, out MessageKind parsedKind) &&
               Enum.IsDefined(typeof(MessageKind), parsedKind)
        ? parsedKind
        : MessageKind.Command;

    Guid? aggregateId = TryGetStoredString(properties, StoredAggregateIdProperty, out var aggregateIdValue) &&
                        Guid.TryParse(aggregateIdValue, out var parsedAggregateId) &&
                        parsedAggregateId != Guid.Empty
        ? parsedAggregateId
        : null;

    DateTimeOffset? occurredOn = TryGetStoredString(properties, StoredOccurredOnProperty, out var occurredOnValue) &&
                                 DateTimeOffset.TryParse(
                                     occurredOnValue,
                                     CultureInfo.InvariantCulture,
                                     DateTimeStyles.RoundtripKind,
                                     out var parsedOccurredOn)
        ? parsedOccurredOn
        : null;

    properties.Remove(StoredMessageKindProperty);
    properties.Remove(StoredAggregateIdProperty);
    properties.Remove(StoredOccurredOnProperty);

    var message = new Message(stored.Body)
    {
      Id = stored.Id,
      Kind = kind,
      AggregateId = aggregateId,
      Properties = properties
    };

    if (occurredOn.HasValue)
      message.OccurredOn = occurredOn.Value;

    var correlationId = stored.CorrelationId;
    if (!correlationId.HasValue &&
        stored.Headers is not null &&
        stored.Headers.TryGetValue(Message.CorrelationHeader, out var correlationValues) &&
        correlationValues.Length == 1 &&
        Guid.TryParse(correlationValues[0], out var parsedCorrelationId))
      correlationId = parsedCorrelationId;

    // CorrelationId owns its header projection. Restore it through the property
    // rather than allowing a stale persisted header to disagree with the property.
    message.CorrelationId = correlationId;
    message.MessageType = stored.MessageType;

    if (stored.Headers is not null)
    {
      foreach (var header in stored.Headers)
      {
        if (string.Equals(
            header.Key,
            Message.CorrelationHeader,
            StringComparison.OrdinalIgnoreCase))
          continue;

        var values = header.Value ?? Array.Empty<string>();
        var safeValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        if (safeValues.Length == 0)
          continue;

        message.Headers[header.Key] = safeValues;
      }
    }

    return message;
  }

  private static bool TryGetStoredString(
      IDictionary<string, object> properties,
      string key,
      out string value)
  {
    if (!properties.TryGetValue(key, out var raw) || raw is null)
    {
      value = string.Empty;
      return false;
    }

    value = raw switch
    {
      string text => text,
      Guid guid => guid.ToString("D"),
      DateTimeOffset timestamp => timestamp.ToString("O", CultureInfo.InvariantCulture),
      DateTime timestamp => new DateTimeOffset(timestamp).ToString("O", CultureInfo.InvariantCulture),
      JsonElement element => element.ValueKind == JsonValueKind.String
          ? element.GetString() ?? string.Empty
          : element.ToString(),
      _ => Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty
    };

    return !string.IsNullOrWhiteSpace(value);
  }
}
