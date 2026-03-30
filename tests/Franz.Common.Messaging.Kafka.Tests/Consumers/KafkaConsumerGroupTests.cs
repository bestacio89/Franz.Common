#nullable enable
using Confluent.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.KafKa.Consumers;
using Microsoft.Extensions.Options;
using FluentAssertions;
using System;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

public class KafkaConsumerGroupTests
{
  // Helper to create the group
  private static KafkaConsumerGroup CreateSutGroup(KafkaMessagingOptions options)
  {
    var opt = Options.Create(options);
    return new KafkaConsumerGroup(opt);
  }

  [Fact]
  public void Should_UseProvidedGroupId_WhenNotNull()
  {
    var expectedGroupId = $"custom-group-{Guid.NewGuid():N}";
    var options = new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = expectedGroupId,
      Consumer = new KafkaConsumerOptions()
    };

    using var group = CreateSutGroup(options);

    group.GroupId.Should().Be(expectedGroupId);
  }

  [Fact]
  public void Should_GenerateFallbackGroupId_WhenNull()
  {
    var options = new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = null,
      Consumer = new KafkaConsumerOptions()
    };

    using var group = CreateSutGroup(options);

    group.GroupId.Should().StartWith("franz-group-");
  }

  [Fact]
  public void Subscribe_And_Unsubscribe_Should_NotThrow()
  {
    var options = new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = null,
      Consumer = new KafkaConsumerOptions()
    };

    using var group = CreateSutGroup(options);

    Action subscribe = () => group.Subscribe("test-topic");
    Action unsubscribe = () => group.Unsubscribe();

    subscribe.Should().NotThrow();
    unsubscribe.Should().NotThrow();
  }

  [Fact]
  public void CreateConsumer_Should_ReturnConsumerInstance()
  {
    var options = new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = null,
      Consumer = new KafkaConsumerOptions()
    };

    using var group = CreateSutGroup(options);

    var consumer = group.CreateConsumer();

    consumer.Should().NotBeNull();
    consumer.Should().BeAssignableTo<IConsumer<Ignore, string>>();
  }

  [Fact]
  public void Dispose_Should_PreventFurtherUsage()
  {
    var options = new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = null,
      Consumer = new KafkaConsumerOptions()
    };

    var group = CreateSutGroup(options);
    group.Dispose();

    Action subscribe = () => group.Subscribe("topic");
    Action unsubscribe = () => group.Unsubscribe();
    Action create = () => _ = group.CreateConsumer();

    subscribe.Should().Throw<ObjectDisposedException>();
    unsubscribe.Should().Throw<ObjectDisposedException>();
    create.Should().Throw<ObjectDisposedException>();
  }

  [Fact]
  public async Task DisposeAsync_Should_PreventFurtherUsage()
  {
    var options = new KafkaMessagingOptions
    {
      BootstrapServers = "localhost:9092",
      GroupId = null,
      Consumer = new KafkaConsumerOptions()
    };

    var group = CreateSutGroup(options);
    await group.DisposeAsync();

    Action subscribe = () => group.Subscribe("topic");
    Action unsubscribe = () => group.Unsubscribe();
    Action create = () => _ = group.CreateConsumer();

    subscribe.Should().Throw<ObjectDisposedException>();
    unsubscribe.Should().Throw<ObjectDisposedException>();
    create.Should().Throw<ObjectDisposedException>();
  }
}