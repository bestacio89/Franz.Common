#nullable enable

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Franz.Common.Messaging.RabbitMQ.Replay;

/// <summary>
/// Rebuilds replay publication properties without losing the original Franz envelope identity.
/// </summary>
internal static class RabbitMQReplayPropertiesMapper
{
  public static BasicProperties Create(
      BasicDeliverEventArgs delivery,
      IDictionary<string, object?> headers)
  {
    ArgumentNullException.ThrowIfNull(delivery);
    ArgumentNullException.ThrowIfNull(headers);

    return new BasicProperties
    {
      DeliveryMode = DeliveryModes.Persistent,
      Headers = headers,
      MessageId = delivery.BasicProperties.MessageId,
      CorrelationId = delivery.BasicProperties.CorrelationId
    };
  }
}
