#nullable enable
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using System.Threading.Channels;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Senders;

[Collection("KafkaSender")]
public class KafkaSenderIntegrationTests(KafkaContainerFixture fixture)
{
  #region Helpers
  // ─── Helpers ──────────────────────────────────────────────────────────────

  private (KafkaSender sender, string topic) BuildSenderWithTopic()
  {
    var uniqueId = Guid.NewGuid().ToString("N")[..8];

    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Name).Returns($"Franz.Tests.{uniqueId}");
    mockAssembly.Setup(a => a.Assembly).Returns(typeof(KafkaSender).Assembly);

    var mockAccessor = new Mock<IAssemblyAccessor>();
    mockAccessor.Setup(a => a.GetEntryAssembly()).Returns(mockAssembly.Object);

    var topic = TopicNamer.GetTopicName(mockAssembly.Object);

    var sender = new KafkaSender(
        Options.Create(new KafkaMessagingOptions { BootStrapServers = fixture.BootstrapServers }),
        new JsonMessageSerializer(),
        mockAccessor.Object,
        NullLogger<KafkaSender>.Instance);

    return (sender, topic);
  }

  private async Task EnsureTopicAsync(string topic)
  {
    using var adminClient = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = fixture.BootstrapServers
    }).Build();

    try
    {
      await adminClient.CreateTopicsAsync([
        new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }
      ]);

      await WaitForTopicMetadataAsync(topic);
    }
    catch (CreateTopicsException e) when (
        e.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
    { }
  }

  private async Task WaitForTopicMetadataAsync(string topic)
  {
    using var adminClient = new AdminClientBuilder(
        new AdminClientConfig { BootstrapServers = fixture.BootstrapServers }).Build();

    var start = DateTime.UtcNow;

    while (DateTime.UtcNow - start < TimeSpan.FromSeconds(10))
    {
      var meta = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(1));

      if (meta.Topics.Any(t => t.Topic == topic && t.Error.Code == ErrorCode.NoError))
        return;

      await Task.Delay(200);
    }
  }

  private (
    TaskCompletionSource assignedTcs,
    Channel<ConsumeResult<string, string>> messages,
    Task consumeTask,
    IConsumer<string, string> consumer)
  BuildReadyConsumer(string topic, string groupId, CancellationToken ct)
  {
    var assignedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    var channel = Channel.CreateUnbounded<ConsumeResult<string, string>>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
    {
      BootstrapServers = fixture.BootstrapServers,
      GroupId = groupId,
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false
    })
    .SetPartitionsAssignedHandler((_, _) => assignedTcs.TrySetResult())
    .Build();

    consumer.Subscribe(topic);

    var consumeTask = Task.Run(() =>
    {
      while (!ct.IsCancellationRequested)
      {
        try
        {
          var result = consumer.Consume(TimeSpan.FromMilliseconds(200));
          if (result?.Message?.Value is not null)
            channel.Writer.TryWrite(result);
        }
        catch (OperationCanceledException) { break; }
        catch { break; }
      }

      channel.Writer.Complete();
    }, ct);

    return (assignedTcs, channel, consumeTask, consumer);
  }

  // 🔥 CORE FIX: deterministic read
  private static async Task<ConsumeResult<string, string>> ReadMessageAsync(
      Channel<ConsumeResult<string, string>> channel,
      Func<ConsumeResult<string, string>, bool> predicate,
      CancellationToken ct)
  {
    var timeout = DateTime.UtcNow.AddSeconds(15);

    while (DateTime.UtcNow < timeout)
    {
      var msg = await channel.Reader.ReadAsync(ct);

      if (predicate(msg))
        return msg;
    }

    throw new TimeoutException("Expected message not found");
  }
  #endregion

  // ─── Tests ────────────────────────────────────────────────────────────────
  [Fact]
  public async Task SendAsync_ShouldSetMessageIdHeader_MatchingMessageId()
  {
    var (sender, topic) = BuildSenderWithTopic();
    await EnsureTopicAsync(topic);

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    var (assignedTcs, messages, _, consumer) =
        BuildReadyConsumer(topic, $"grp-{topic}", cts.Token);

    try
    {
      await assignedTcs.Task.WaitAsync(TimeSpan.FromSeconds(30), cts.Token);

      await using (sender)
      {
        var message = new Message("msg-id-test");

        await sender.SendAsync(message);

        var result = await ReadMessageAsync(
            messages,
            m =>
            {
              if (!m.Message.Headers.TryGetLastBytes("X-Message-ID", out var bytes))
                return false;

              return Encoding.UTF8.GetString(bytes) == message.Id.ToString();
            },
            cts.Token);

        Encoding.UTF8.GetString(result.Message.Headers.GetLastBytes("X-Message-ID"))
            .Should().Be(message.Id.ToString());
      }
    }
    finally
    {
      cts.Cancel();
      consumer.Close();
      consumer.Dispose();
    }
  }

  [Fact]
  public async Task SendAsync_ShouldIncludeCustomHeaders_OnWireMessage()
  {
    var (sender, topic) = BuildSenderWithTopic();
    await EnsureTopicAsync(topic);

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    var (assignedTcs, messages, _, consumer) =
        BuildReadyConsumer(topic, $"grp-{topic}", cts.Token);

    try
    {
      await assignedTcs.Task.WaitAsync(TimeSpan.FromSeconds(30), cts.Token);

      await using (sender)
      {
        var tenantId = Guid.CreateVersion7().ToString();

        var message = new Message("headers-test");
        message.Headers["X-Tenant-ID"] = [tenantId];

        await sender.SendAsync(message);

        var result = await ReadMessageAsync(
            messages,
            m => m.Message.Key == message.CorrelationId.ToString(),
            cts.Token);

        result.Message.Headers.TryGetLastBytes("X-Tenant-ID", out var headerBytes)
            .Should().BeTrue();

        Encoding.UTF8.GetString(headerBytes!).Should().Be(tenantId);
      }
    }
    finally
    {
      await cts.CancelAsync();
      consumer.Close();
      consumer.Dispose();
    }
  }


  [Fact]
  public async Task SendAsync_ShouldUseCorrelationIdAsKey_ForPartitionAffinity()
  {
    var (sender, topic) = BuildSenderWithTopic();
    await EnsureTopicAsync(topic);

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    var (assignedTcs, messages, _, consumer) =
        BuildReadyConsumer(topic, $"grp-{topic}", cts.Token);

    try
    {
      await assignedTcs.Task.WaitAsync(TimeSpan.FromSeconds(30), cts.Token);

      await using (sender)
      {
        var message = new Message("affinity-test");

        await sender.SendAsync(message);

        var result = await ReadMessageAsync(
            messages,
            m => m.Message.Key == message.CorrelationId.ToString(),
            cts.Token);

        result.Message.Key.Should().Be(message.CorrelationId.ToString());
      }
    }
    finally
    {
      cts.Cancel();
      consumer.Close();
      consumer.Dispose();
    }
  }

  [Fact]
  public async Task SendAsync_ShouldProduceMultipleMessages_InOrder()
  {
    var (sender, topic) = BuildSenderWithTopic();
    await EnsureTopicAsync(topic);

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    var assignedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var channel = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    const int messageCount = 3;

    var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
    {
      BootstrapServers = fixture.BootstrapServers,
      GroupId = $"grp-order-{Guid.NewGuid():N}",
      AutoOffsetReset = AutoOffsetReset.Earliest
    })
    .SetPartitionsAssignedHandler((_, _) => assignedTcs.TrySetResult())
    .Build();

    consumer.Subscribe(topic);

    var consumeTask = Task.Run(() =>
    {
      while (!cts.Token.IsCancellationRequested)
      {
        try
        {
          var result = consumer.Consume(TimeSpan.FromMilliseconds(200));
          if (result?.Message?.Value is not null)
            channel.Writer.TryWrite(result.Message.Value);
        }
        catch (OperationCanceledException) { break; }
        catch { break; }
      }
      channel.Writer.Complete();
    }, cts.Token);

    try
    {
      await assignedTcs.Task.WaitAsync(TimeSpan.FromSeconds(30), cts.Token);

      await using (sender)
      {
        for (var i = 1; i <= messageCount; i++)
          await sender.SendAsync(new Message($"ordered-message-{i}"));

        var received = new List<string>();
        for (var i = 0; i < messageCount; i++)
        {
          var value = await channel.Reader.ReadAsync(cts.Token)
              .AsTask().WaitAsync(TimeSpan.FromSeconds(20), cts.Token);
          received.Add(value);
        }

        received.Should().HaveCount(messageCount);
      }
    }
    finally
    {
      cts.Cancel();
      consumer.Close();
      consumer.Dispose();
    }
  }
}