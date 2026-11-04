using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests;

public sealed class MessagingCapabilitiesTests
{
  [Fact]
  public void RabbitMQCapabilities_ShouldDeclareFifoAndQueueControlsButNotPartitionedOrdering()
  {
    var capabilities = RabbitMQMessagingCapabilities.Descriptor;

    capabilities.TransportName.Should().Be("RabbitMQ");
    capabilities.Supports(MessagingCapability.OrderedConsumption).Should().BeTrue();
    capabilities.Supports(MessagingCapability.DurableDelivery).Should().BeTrue();
    capabilities.Supports(MessagingCapability.BoundedDestination).Should().BeTrue();
    capabilities.Supports(MessagingCapability.PartitionedConsumption).Should().BeFalse();
    capabilities.GetUnsupportedReason(MessagingCapability.PartitionedConsumption).Should().NotBeNullOrWhiteSpace();
  }
}
