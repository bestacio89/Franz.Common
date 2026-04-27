using Franz.Common.Messaging.Headers;
using RabbitMQ.Client;
using System.Linq;

namespace Franz.Common.Messaging.RabbitMQ.Extensions;

public static class MessageHeadersExtensions
{
  public static IBasicProperties ToBasicProperties(this MessageHeaders messageHeaders)
  {
    var properties = new BasicProperties
    {
      Headers = new Dictionary<string, object?>()
    };

    foreach (var header in messageHeaders)
    {
      var value = header.Value.FirstOrDefault();

      if (!string.IsNullOrWhiteSpace(value))
      {
        properties.Headers[header.Key] = value!;
      }
    }

    return properties;
  }
}