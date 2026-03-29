#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Hosting;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

[Collection("KafkaConsumer")]
public sealed class KafkaConsumerFactoryUnitTests
{
  private static KafkaConsumerFactory BuildFactory(string bootstrapServers, string groupId) =>
      new(Options.Create(new KafkaMessagingOptions
      {
        BootStrapServers = bootstrapServers,
        GroupID = groupId
      }), NullLogger<KafkaConsumerFactory>.Instance);

  [Fact]
  public void Build_ShouldThrow_WhenBootstrapServersIsNull()
  {
    var factory = BuildFactory(null!, "group-id");
    var act = () => factory.Build();
    act.Should().Throw<ArgumentException>()
        .WithMessage("*BootStrapServers*");
  }

  [Fact]
  public void Build_ShouldThrow_WhenBootstrapServersIsEmpty()
  {
    var factory = BuildFactory(string.Empty, "group-id");
    var act = () => factory.Build();
    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void Build_ShouldThrow_WhenBootstrapServersIsWhitespace()
  {
    var factory = BuildFactory("   ", "group-id");
    var act = () => factory.Build();
    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void Build_ShouldThrow_WhenGroupIdIsNull()
  {
    var factory = BuildFactory("localhost:9092", null!);
    var act = () => factory.Build();
    act.Should().Throw<ArgumentException>()
        .WithMessage("*GroupID*");
  }

  [Fact]
  public void Build_ShouldThrow_WhenGroupIdIsEmpty()
  {
    var factory = BuildFactory("localhost:9092", string.Empty);
    var act = () => factory.Build();
    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void Build_ShouldThrow_WhenGroupIdIsWhitespace()
  {
    var factory = BuildFactory("localhost:9092", "   ");
    var act = () => factory.Build();
    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void Build_ShouldReturnConsumer_WhenConfigurationIsValid()
  {
    // A real consumer can be built without a live broker —
    // the connection is lazy and only established on Subscribe()/Consume().
    var factory = BuildFactory("localhost:9092", "test-group");
    using var consumer = factory.Build();
    consumer.Should().NotBeNull();
    consumer.Should().BeAssignableTo<IConsumer<string, string>>();
  }

  [Fact]
  public void Build_ShouldReturnDistinctInstances_OnMultipleCalls()
  {
    // Factory must not cache or reuse consumer instances — each call
    // should produce an independent consumer with its own state.
    var factory = BuildFactory("localhost:9092", "test-group");
    using var first = factory.Build();
    using var second = factory.Build();
    first.Should().NotBeSameAs(second);
  }
}