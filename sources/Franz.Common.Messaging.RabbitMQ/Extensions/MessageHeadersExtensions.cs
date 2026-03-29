using Franz.Common.Messaging.Headers;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Linq;

namespace Franz.Common.Messaging.RabbitMQ.Extensions;

public static class MessageHeadersExtensions
{
    public static IBasicProperties ToBasicProperties(this MessageHeaders messageHeaders)
    {
        var properties = new BasicProperties();
        properties.Headers = new Dictionary<string, object>();

        foreach (var header in messageHeaders)
        {
            properties.Headers[header.Key] = header.Value.FirstOrDefault();
        }

        return properties;
    }
}
