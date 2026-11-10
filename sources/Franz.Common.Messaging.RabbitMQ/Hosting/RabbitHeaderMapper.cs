#nullable enable

using Franz.Common.Messaging.Messages;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Franz.Common.Messaging.RabbitMQ.Hosting;

/// <summary>
/// Maps RabbitMQ delivery metadata into the transport-neutral message headers.
/// </summary>
public static class RabbitHeaderMapper
{
  public static IDictionary<string, string[]> ExtractHeaders(BasicDeliverEventArgs eventArgs)
  {
    ArgumentNullException.ThrowIfNull(eventArgs);

    var headers = new Dictionary<string, string[]>();
    var rabbitHeaders = eventArgs.BasicProperties.Headers;

    if (rabbitHeaders is not null)
    {
      foreach (var header in rabbitHeaders)
      {
        // Keep and normalize causation: it is part of the message envelope, not
        // disposable RabbitMQ infrastructure metadata.
        if (string.Equals(header.Key, Message.CausationHeader, StringComparison.OrdinalIgnoreCase))
        {
          headers[Message.CausationHeader] = MapValue(header.Value);
          continue;
        }

        if (header.Key.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
          continue;

        headers[header.Key] = MapValue(header.Value);
      }
    }

    // CorrelationId is an AMQP basic property, not an entry in the Headers table.
    if (!string.IsNullOrWhiteSpace(eventArgs.BasicProperties.CorrelationId))
      headers[Message.CorrelationHeader] = [eventArgs.BasicProperties.CorrelationId];

    return headers;
  }

  private static string[] MapValue(object? value)
  {
    return value switch
    {
      byte[] bytes => [Encoding.UTF8.GetString(bytes)],
      IEnumerable<object> list => list.SelectMany(MapValue).ToArray(),
      null => [],
      _ => [value.ToString() ?? string.Empty]
    };
  }
}
