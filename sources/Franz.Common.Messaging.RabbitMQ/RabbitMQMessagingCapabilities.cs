using Franz.Common.Messaging.Configuration;
using System.Collections.Generic;

namespace Franz.Common.Messaging.RabbitMQ;

/// <summary>Capability declaration for the RabbitMQ messaging adapter.</summary>
public static class RabbitMQMessagingCapabilities
{
  public static MessagingTransportCapabilities Descriptor { get; } = new(
      "RabbitMQ",
      new[]
      {
        MessagingCapability.Publish,
        MessagingCapability.Consume,
        MessagingCapability.Retry,
        MessagingCapability.DeadLetter,
        MessagingCapability.OrderedConsumption,
        MessagingCapability.TransactionalPublishing,
        MessagingCapability.DurableDelivery,
        MessagingCapability.BoundedDestination,
        MessagingCapability.DeliveryAttemptLimit,
        MessagingCapability.PublisherAcknowledgements,
        MessagingCapability.DestinationProvisioning,
        MessagingCapability.ProvisioningVerification
      },
      new Dictionary<MessagingCapability, string>
      {
        [MessagingCapability.MessageKeyOrdering] = "Per-key routing and ordering are not provided by the default queue topology.",
        [MessagingCapability.AggregateOrdering] = "Aggregate-key partitioning is not provided by the default queue topology.",
        [MessagingCapability.PartitionedConsumption] = "The adapter uses queue consumers and does not expose Kafka-style partition assignment."
      });
}
