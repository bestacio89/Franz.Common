using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

public sealed class MessagingCapabilitiesTests
{
  [Fact]
  public void KafkaCapabilities_ShouldDeclarePartitionedAndKeyedOrderingButNotGlobalFifo()
  {
    var capabilities = KafkaMessagingCapabilities.Descriptor;

    capabilities.TransportName.Should().Be("Kafka");
    capabilities.Supports(MessagingCapability.TransactionalPublishing).Should().BeTrue();
    capabilities.Supports(MessagingCapability.AggregateOrdering).Should().BeTrue();
    capabilities.Supports(MessagingCapability.PartitionedConsumption).Should().BeTrue();
    capabilities.Supports(MessagingCapability.OrderedConsumption).Should().BeFalse();
    capabilities.GetUnsupportedReason(MessagingCapability.OrderedConsumption).Should().NotBeNullOrWhiteSpace();
  }
}
