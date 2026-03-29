#nullable enable
using Franz.Common.Messaging.Headers;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Franz.Common.Messaging.RabbitMQ.Hosting;

/// <summary>
/// High-performance mapper for RabbitMQ headers.
/// Converts RabbitMQ header dictionary into MessageHeaders (transport-agnostic).
/// Optimized for .NET 10 to minimize allocations during message consumption.
/// </summary>
public static class RabbitHeaderMapper
{
  public static IDictionary<string, string[]> ExtractHeaders(BasicDeliverEventArgs eventArgs)
  {
    var headers = new Dictionary<string, string[]>();

    if (eventArgs.BasicProperties.Headers == null)
      return headers;

    foreach (var header in eventArgs.BasicProperties.Headers)
    {
      // Franz.Common Rule: Skip internal infrastructure headers
      if (header.Key.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      headers[header.Key] = MapValue(header.Value);
    }

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
