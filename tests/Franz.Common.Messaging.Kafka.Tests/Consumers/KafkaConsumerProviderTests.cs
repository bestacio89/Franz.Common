#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.KafKa.Consumers;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

[Collection("KafkaConsumer")]
public sealed class KafkaConsumerProviderTests(KafkaContainerFixture fixture)
{
  private KafkaConsumerProvider CreateProvider()
  {
    var cleanedAddress = fixture.BootstrapServers
        .Replace("plaintext://", "", StringComparison.OrdinalIgnoreCase)
        .TrimEnd('/');

    var options = Options.Create(new KafkaMessagingOptions
    {
      BootStrapServers = cleanedAddress,
      GroupID = $"provider-test-{Guid.NewGuid():N}"
    });

    return new KafkaConsumerProvider(options, NullLogger<KafkaConsumerProvider>.Instance);
  }

  [Fact]
  public void CreateConsumer_Should_Create_New_Instance()
  {
    var provider = CreateProvider();
    using var c1 = provider.CreateConsumer();
    using var c2 = provider.CreateConsumer();
    c1.Should().NotBeSameAs(c2);
  }

  [Fact]
  public void CreateConsumer_Should_Return_Valid_Handle()
  {
    var provider = CreateProvider();
    using var consumer = provider.CreateConsumer();
    consumer.Handle.Should().NotBeNull();
    // FIX for CS1061:
    Library.Version.Should().NotBe(0);
  }
}