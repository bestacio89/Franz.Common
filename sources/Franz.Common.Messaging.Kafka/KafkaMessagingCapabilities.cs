using Franz.Common.Messaging.Configuration;
using System.Collections.Generic;

namespace Franz.Common.Messaging.Kafka;

/// <summary>Capability declaration for the Kafka messaging adapter.</summary>
public static class KafkaMessagingCapabilities
{
  public static MessagingTransportCapabilities Descriptor { get; } = new(
      "Kafka",
      new[]
      {
        MessagingCapability.Publish,
        MessagingCapability.Consume,
        MessagingCapability.Retry,
        MessagingCapability.DeadLetter,
        MessagingCapability.MessageKeyOrdering,
        MessagingCapability.AggregateOrdering,
        MessagingCapability.TransactionalPublishing,
        MessagingCapability.PartitionedConsumption,
        MessagingCapability.PublisherAcknowledgements,
        MessagingCapability.DestinationProvisioning,
        MessagingCapability.ProvisioningVerification,
        MessagingCapability.DeliveryAttemptLimit
      },
      new Dictionary<MessagingCapability, string>
      {
        [MessagingCapability.OrderedConsumption] = "Ordering is partition-scoped and requires a stable message key; global FIFO across partitions is not guaranteed.",
        [MessagingCapability.DurableDelivery] = "Durability is governed by topic replication and broker configuration rather than durable queue declarations.",
        [MessagingCapability.BoundedDestination] = "Queue-style maximum message and byte limits are not exposed as transport-neutral destination guarantees."
      });
}
