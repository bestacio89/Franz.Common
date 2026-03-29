#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.KafKa.Consumers;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

[Collection("KafkaConsumer")]
public sealed class KafkaConsumerGroupTests(KafkaContainerFixture fixture)
{
  private KafkaConsumerGroup CreateSutGroup() // Unique Helper Name
  {
    var cleanedAddress = fixture.BootstrapServers
        .Replace("plaintext://", "", StringComparison.OrdinalIgnoreCase)
        .TrimEnd('/');

    var options = Options.Create(new KafkaMessagingOptions
    {
      BootStrapServers = cleanedAddress,
      GroupID = $"test-group-{Guid.NewGuid():N}"
    });

    return new KafkaConsumerGroup(options);
  }

  [Fact]
  public void CreateConsumer_Should_Return_Same_Instance()
  {
    using var group = CreateSutGroup();
    var c1 = group.CreateConsumer();
    var c2 = group.CreateConsumer();
    c1.Should().BeSameAs(c2);
  }

  [Fact]
  public void Subscribe_Should_Not_Throw()
  {
    using var group = CreateSutGroup();
    // Act: Subscribe returns void, so we just call it.
    group.Subscribe("test-topic");
  }
}