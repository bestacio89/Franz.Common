#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Franz.Common.Messaging.Kafka;
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
            GroupId = $"test-group-{Guid.NewGuid():N}",
            BootstrapServers = _fixture.BootstrapServers,
            Consumer = new KafkaConsumerOptions
            {
              
              EnableAutoCommit = false
            }
          }),
          NullLogger<KafkaConsumerFactory>.Instance);

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
  public async Task Consumer_ShouldReceiveMessagePublishedToTopic()
  {
    var factory = BuildFactory();
    var topic = $"test-publish-{Guid.NewGuid():N}";
    var tcs = new TaskCompletionSource<string>();

    using var consumer = factory.Build();
    consumer.Subscribe(topic);

    // Produce a test message using real Kafka producer
    using var producer = new ProducerBuilder<string, string>(
        new ProducerConfig { BootstrapServers = _fixture.BootstrapServers }).Build();

    _ = Task.Run(() =>
    {
      var msg = consumer.Consume(TimeSpan.FromSeconds(10));
      if (msg != null)
        tcs.TrySetResult(msg.Message.Value);
    });

    var messageValue = "hello-world";
    await producer.ProduceAsync(topic, new Message<string, string> { Key = "key", Value = messageValue });

    var received = await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
    received.Should().Be(messageValue);

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