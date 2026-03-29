#nullable enable
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Extensions;

public static class RabbitMQHeadersExtensions
{
  /// <summary>
  /// Maps our transport-neutral IDictionary to RabbitMQ's native BasicProperties.
  /// Senior Note: Keeps RabbitMQ dependencies out of the core Messaging project.
  /// </summary>
  public static BasicProperties ToBasicProperties(this IDictionary<string, string[]> headers)
  {
    var props = new BasicProperties();
    var rabbitHeaders = new Dictionary<string, object?>();

    foreach (var (key, values) in headers)
    {
      if (values == null || values.Length == 0) continue;

      // Standardize: Single value becomes a string, multi-value becomes a list
      if (values.Length == 1)
        rabbitHeaders[key] = values[0];
      else
        rabbitHeaders[key] = values.Cast<object>().ToList();
    }

    props.Headers = rabbitHeaders;
    return props;
  }
}