using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Franz.Common.Messaging.Configuration;

/// <summary>
/// Broker-neutral capabilities that a transport adapter can guarantee.
/// </summary>
public enum MessagingCapability
{
  Publish,
  Consume,
  Retry,
  DeadLetter,
  OrderedConsumption,
  MessageKeyOrdering,
  AggregateOrdering,
  TransactionalPublishing,
  PartitionedConsumption,
  DurableDelivery,
  BoundedDestination,
  DeliveryAttemptLimit,
  PublisherAcknowledgements,
  DestinationProvisioning,
  ProvisioningVerification
}

/// <summary>
/// Explicit capability declaration for a messaging transport adapter.
/// Unsupported capabilities include a reason so callers can avoid relying on
/// behavior that the adapter cannot guarantee.
/// </summary>
public sealed class MessagingTransportCapabilities
{
  public MessagingTransportCapabilities(
      string transportName,
      IEnumerable<MessagingCapability> supportedCapabilities,
      IReadOnlyDictionary<MessagingCapability, string> unsupportedCapabilities)
  {
    if (string.IsNullOrWhiteSpace(transportName))
      throw new ArgumentException("Transport name cannot be empty.", nameof(transportName));
    ArgumentNullException.ThrowIfNull(supportedCapabilities);
    ArgumentNullException.ThrowIfNull(unsupportedCapabilities);

    TransportName = transportName;
    var supported = supportedCapabilities.Distinct().Order().ToArray();
    var unsupported = new Dictionary<MessagingCapability, string>();

    foreach (var (capability, reason) in unsupportedCapabilities)
    {
      if (string.IsNullOrWhiteSpace(reason))
        throw new ArgumentException($"Unsupported capability '{capability}' requires an explanation.", nameof(unsupportedCapabilities));
      if (!unsupported.TryAdd(capability, reason))
        throw new ArgumentException($"Unsupported capability '{capability}' was declared more than once.", nameof(unsupportedCapabilities));
    }

    var overlap = supported.Intersect(unsupported.Keys).ToArray();
    if (overlap.Length > 0)
      throw new ArgumentException($"Capabilities cannot be both supported and unsupported: {string.Join(", ", overlap)}.");

    SupportedCapabilities = Array.AsReadOnly(supported);
    UnsupportedCapabilities = new ReadOnlyDictionary<MessagingCapability, string>(unsupported);
  }

  public string TransportName { get; }

  public IReadOnlyList<MessagingCapability> SupportedCapabilities { get; }

  public IReadOnlyDictionary<MessagingCapability, string> UnsupportedCapabilities { get; }

  public bool Supports(MessagingCapability capability)
      => SupportedCapabilities.Contains(capability);

  public string? GetUnsupportedReason(MessagingCapability capability)
      => UnsupportedCapabilities.TryGetValue(capability, out var reason) ? reason : null;
}
