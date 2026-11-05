#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

[Collection("KafkaConsumer")]
public sealed class KafkaConsumerFactoryIntegrationTests : IClassFixture<KafkaContainerFixture>
{
  private readonly KafkaContainerFixture _fixture;

  public KafkaConsumerFactoryIntegrationTests(KafkaContainerFixture fixture)
  {
    _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
  }

  private KafkaConsumerFactory BuildFactory(string? groupId = null) =>
      new KafkaConsumerFactory(
          Options.Create(new KafkaMessagingOptions
          {
            GroupId = groupId ?? $"test-group-{Guid.NewGuid():N}",
            BootstrapServers = _fixture.BootstrapServers,
            Security = new KafkaSecurityOptions { AllowInsecureTransport = true },
            Consumer = new KafkaConsumerOptions
            {
              EnableAutoCommit = false,
              SessionTimeoutMs = 6_000,
              MaxPollIntervalMs = 30_000
            }
          }),
          NullLogger<KafkaConsumerFactory>.Instance,
          new KafkaRebalanceCoordinator());

  private static Task DriveUntilAssigned(IConsumer<string, string> consumer,
      TaskCompletionSource assignedTcs,
      CancellationToken ct) =>
      Task.Run(() =>
      {
        while (!assignedTcs.Task.IsCompleted && !ct.IsCancellationRequested)
        {
          try { consumer.Consume(TimeSpan.FromMilliseconds(100)); }
          catch (OperationCanceledException) { break; }
          catch { break; }
        }
      }, ct);

  [Fact]
  public void Factory_Should_BuildValidConsumer()
  {
    var factory = BuildFactory();
    using var consumer = factory.Build();

    var topic = $"test-topic-{Guid.NewGuid():N}";
    var act = () => consumer.Subscribe(topic);
    act.Should().NotThrow();
    consumer.Unsubscribe();
  }

  [Fact]
  public void MultipleConsumers_ShouldBeIndependent()
  {
    var factory = BuildFactory();

    using var c1 = factory.Build();
    using var c2 = factory.Build();

    var topic = $"test-indep-{Guid.NewGuid():N}";
    c1.Subscribe(topic);
    c2.Subscribe(topic);

    // Dispose first — second remains valid
    c1.Unsubscribe();
    c1.Close();

    var act = () => c2.Unsubscribe();
    act.Should().NotThrow();

    c2.Close();
  }

  [Fact]
  public void Consumer_ShouldNotCreateUnknownTopicsOnSubscription()
  {
    var factory = BuildFactory();
    var topic = $"unknown-topic-{Guid.NewGuid():N}";

    using var consumer = factory.Build();
    consumer.Subscribe(topic);

    try
    {
      _ = consumer.Consume(TimeSpan.FromSeconds(2));
    }
    catch (ConsumeException exception)
        when (exception.Error.Code is ErrorCode.UnknownTopicOrPart or ErrorCode.LeaderNotAvailable)
    {
      // Both are valid broker responses while metadata for an unknown topic is unavailable.
    }

    using var admin = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = _fixture.BootstrapServers
    }).Build();

    var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
    metadata.Topics.Should().NotContain(
        candidate => StringComparer.Ordinal.Equals(candidate.Topic, topic),
        "subscribing to an unknown topic must not provision it");
  }

  [Fact]
  public async Task ConsumerGroup_RebalanceShouldRedeliverUncommittedMessageToNewOwner()
  {
    var groupId = $"rebalance-group-{Guid.NewGuid():N}";
    var topic = $"rebalance-topic-{Guid.NewGuid():N}";

    using var admin = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = _fixture.BootstrapServers
    }).Build();

    await admin.CreateTopicsAsync(new[]
    {
      new TopicSpecification
      {
        Name = topic,
        NumPartitions = 2,
        ReplicationFactor = 1
      }
    });

    using var producer = new ProducerBuilder<string, string>(
        new ProducerConfig { BootstrapServers = _fixture.BootstrapServers }).Build();

    await producer.ProduceAsync(
        new TopicPartition(topic, new Partition(0)),
        new Message<string, string> { Key = "rebalance-key-0", Value = "uncommitted-payload-0" });
    await producer.ProduceAsync(
        new TopicPartition(topic, new Partition(1)),
        new Message<string, string> { Key = "rebalance-key-1", Value = "uncommitted-payload-1" });

    var factory = BuildFactory(groupId);
    using var firstOwner = factory.Build();
    using var nextOwner = factory.Build();

    firstOwner.Subscribe(topic);
    var firstDeliveries = new List<ConsumeResult<string, string>>(capacity: 2);
    var assignmentDeadline = DateTime.UtcNow.AddSeconds(20);
    while ((firstOwner.Assignment.Count != 2 || firstDeliveries.Count != 2) &&
           DateTime.UtcNow < assignmentDeadline)
    {
      var candidate = firstOwner.Consume(TimeSpan.FromMilliseconds(100));
      if (candidate is not null &&
          firstDeliveries.All(delivery => delivery.TopicPartition != candidate.TopicPartition))
      {
        firstDeliveries.Add(candidate);
      }
    }

    firstOwner.Assignment.Should().HaveCount(2);
    firstDeliveries.Should().HaveCount(2);
    firstDeliveries.Select(delivery => delivery.TopicPartition).Should().OnlyHaveUniqueItems();

    // Neither offset is committed. The second member forces a real rebalance;
    // whichever partition moves must be delivered again to its new owner.
    nextOwner.Subscribe(topic);

    ConsumeResult<string, string>? redelivered = null;
    var rebalanceDeadline = DateTime.UtcNow.AddSeconds(30);
    while (DateTime.UtcNow < rebalanceDeadline)
    {
      _ = firstOwner.Consume(TimeSpan.FromMilliseconds(100));
      var candidate = nextOwner.Consume(TimeSpan.FromMilliseconds(100));
      if (candidate is not null)
        redelivered = candidate;

      if (firstOwner.Assignment.Count == 1 &&
          nextOwner.Assignment.Count == 1 &&
          redelivered is not null)
      {
        break;
      }
    }

    firstOwner.Assignment.Should().ContainSingle();
    nextOwner.Assignment.Should().ContainSingle();
    redelivered.Should().NotBeNull("the new owner must receive an uncommitted partition offset again");

    var originalDelivery = firstDeliveries.Single(
        delivery => delivery.TopicPartition == redelivered!.TopicPartition);
    redelivered!.Message.Value.Should().Be(originalDelivery.Message.Value);
    redelivered.TopicPartitionOffset.Should().Be(originalDelivery.TopicPartitionOffset);

    nextOwner.Commit(redelivered);
    KafkaMessagingCapabilities.Descriptor.Supports(MessagingCapability.PartitionedConsumption)
        .Should().BeTrue();
  }

  [Fact]
  public async Task ConsumerGroup_ShouldDistributePartitionsAcrossMembers()
  {
    var groupId = $"partition-group-{Guid.NewGuid():N}";
    var topic = $"partition-topic-{Guid.NewGuid():N}";

    using var admin = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = _fixture.BootstrapServers
    }).Build();

    await admin.CreateTopicsAsync(new[]
    {
      new TopicSpecification
      {
        Name = topic,
        NumPartitions = 2,
        ReplicationFactor = 1
      }
    });

    var factory = BuildFactory(groupId);
    using var firstMember = factory.Build();
    using var secondMember = factory.Build();
    firstMember.Subscribe(topic);
    secondMember.Subscribe(topic);

    var deadline = DateTime.UtcNow.AddSeconds(30);
    while (DateTime.UtcNow < deadline &&
           (firstMember.Assignment.Count != 1 || secondMember.Assignment.Count != 1))
    {
      _ = firstMember.Consume(TimeSpan.FromMilliseconds(100));
      _ = secondMember.Consume(TimeSpan.FromMilliseconds(100));
    }

    firstMember.Assignment.Should().ContainSingle();
    secondMember.Assignment.Should().ContainSingle();
    firstMember.Assignment.Single().Topic.Should().Be(topic);
    secondMember.Assignment.Single().Topic.Should().Be(topic);
    firstMember.Assignment.Single().Should().NotBe(secondMember.Assignment.Single());

    KafkaMessagingCapabilities.Descriptor.Supports(MessagingCapability.PartitionedConsumption)
        .Should().BeTrue();
  }

  [Fact]
  public async Task Consumer_ShouldReceiveMessagePublishedToTopic()
  {
    var factory = BuildFactory();
    var topic = $"test-publish-{Guid.NewGuid():N}";

    using var admin = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = _fixture.BootstrapServers
    }).Build();

    await admin.CreateTopicsAsync(new[]
    {
      new TopicSpecification
      {
        Name = topic,
        NumPartitions = 1,
        ReplicationFactor = 1
      }
    });

    using var consumer = factory.Build();
    consumer.Subscribe(topic);

    using var producer = new ProducerBuilder<string, string>(
        new ProducerConfig { BootstrapServers = _fixture.BootstrapServers }).Build();

    var consumeTask = Task.Run(() => consumer.Consume(TimeSpan.FromSeconds(10)));
    const string messageValue = "hello-world";

    await producer.ProduceAsync(topic, new Message<string, string>
    {
      Key = "key",
      Value = messageValue
    });

    var received = await consumeTask.TimeoutAfter(TimeSpan.FromSeconds(15));
    received.Should().NotBeNull();
    received!.Message.Value.Should().Be(messageValue);

    consumer.Unsubscribe();
  }
}

// Extension for Task timeout in tests
public static class TaskExtensions
{
  public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
  {
    using var cts = new CancellationTokenSource();
    var delayTask = Task.Delay(timeout, cts.Token);
    var completedTask = await Task.WhenAny(task, delayTask);
    if (completedTask == delayTask) throw new TimeoutException();
    cts.Cancel();
    return await task;
  }
}
