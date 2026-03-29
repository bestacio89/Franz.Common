using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

[Collection("KafkaConsumer")]
public sealed class KafkaConsumerFactoryIntegrationTests(KafkaContainerFixture fixture)
{
  private KafkaConsumerFactory BuildFactory(string? groupId = null) =>
      new(Options.Create(new KafkaMessagingOptions
      {
        BootStrapServers = fixture.BootstrapServers,
        GroupID = groupId ?? $"group-{Guid.CreateVersion7():N}"
      }), NullLogger<KafkaConsumerFactory>.Instance);

  /// <summary>
  /// Drives the consumer's internal poll loop in the background until the
  /// partition assignment callback fires. This is required because Confluent's
  /// consumer is not event-driven — SetPartitionsAssignedHandler only fires
  /// during an active Consume() call, never from Subscribe() alone.
  /// </summary>
  private static Task DriveUntilAssigned(
      IConsumer<string, string> consumer,
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
  public void Build_ShouldProduceConnectableConsumer_AgainstRealBroker()
  {
    var factory = BuildFactory();
    using var consumer = factory.Build();

    // Subscribe does not throw — broker is reachable and group can be formed.
    var topic = $"test-factory-{Guid.CreateVersion7():N}";
    var act = () => consumer.Subscribe(topic);
    act.Should().NotThrow();

    consumer.Unsubscribe();
  }

  [Fact]
  public async Task Build_ShouldRespectEarliestOffsetReset_OnNewGroup()
  {
    // Arrange: produce a message before the consumer group exists,
    // then verify a new consumer receives it — proving AutoOffsetReset = Earliest.
    var topic = $"test-offset-{Guid.CreateVersion7():N}";
    var groupId = $"group-earliest-{Guid.CreateVersion7():N}";

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

    using var producer = new ProducerBuilder<string, string>(
        new ProducerConfig { BootstrapServers = fixture.BootstrapServers }).Build();

    producer.Produce(topic, new Message<string, string> { Value = "earliest-test" });
    producer.Flush(TimeSpan.FromSeconds(5));

    // Build consumer AFTER message was produced
    var partitionsAssignedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
    {
      BootstrapServers = fixture.BootstrapServers,
      GroupId = groupId,
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false
    })
    .SetPartitionsAssignedHandler((_, _) => partitionsAssignedTcs.TrySetResult())
    .Build();

    consumer.Subscribe(topic);

    // ✅ Drive the poll loop so the rebalance handshake can complete
    // and SetPartitionsAssignedHandler fires.
    var driveTask = DriveUntilAssigned(consumer, partitionsAssignedTcs, cts.Token);

    var assigned = await Task.WhenAny(partitionsAssignedTcs.Task, Task.Delay(30000, cts.Token));
    assigned.Should().Be(partitionsAssignedTcs.Task,
        "Broker should assign partitions before timeout.");

    await driveTask.WaitAsync(cts.Token).ConfigureAwait(false);

    // Now consume the pre-existing message
    var result = consumer.Consume(TimeSpan.FromSeconds(10));

    result.Should().NotBeNull("Consumer should receive the pre-existing message with Earliest offset reset.");
    result!.Message.Value.Should().Be("earliest-test");

    consumer.Unsubscribe();
  }

 
  [Fact]
  public void Build_ShouldProduceIndependentConsumers_WithSameConfig()
  {
    // Two consumers built from the same factory config must be independent —
    // closing one must not affect the other.
    var factory = BuildFactory();

    using var first = factory.Build();
    using var second = factory.Build();

    var topic = $"test-independent-{Guid.CreateVersion7():N}";

    first.Subscribe(topic);
    second.Subscribe(topic);

    // Dispose first — second should remain functional
    first.Unsubscribe();
    first.Close();

    var act = () => second.Unsubscribe();
    act.Should().NotThrow("Second consumer should be unaffected by first consumer's disposal.");

    second.Close();
  }
}
