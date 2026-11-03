#nullable enable
using System.Text;
using Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging;
using Franz.Common.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Integration;

[Collection("RabbitMQ Integration")]
public sealed class RabbitMQIntegrationTests(RabbitMQHostingFixture fixture) : IClassFixture<RabbitMQHostingFixture>
{
  private readonly RabbitMQHostingFixture _fixture = fixture;

  [Fact]
  [Trait("Category", "Connectivity")]
  public async Task Infrastructure_ShouldConnect_ViaUriBootstrap()
  {
    var channelPool = _fixture.Services.GetRequiredService<IChannelPool>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    var channel = await channelPool.GetAsync(cts.Token);
    try
    {
      channel.Should().NotBeNull();
      channel.IsOpen.Should().BeTrue();
    }
    finally
    {
      channelPool.Return(channel);
    }
  }

  [Fact]
  [Trait("Category", "FaultTolerance")]
  public async Task Listener_ShouldTriggerReplayStrategy_OnHandlerFailure()
  {
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();
    var channelPool = _fixture.Services.GetRequiredService<IChannelPool>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

    listener.OnMessageReceivedAsync = _ => throw new Exception("Simulated Processing Failure");

    try
    {
      await listener.Listen(cts.Token);
      await listener.WaitUntilReadyAsync().WaitAsync(cts.Token);

      var pubChannel = await channelPool.GetAsync(cts.Token);
      try
      {
        var queueName = QueueNamer.GetQueueName(
            _fixture.Services.GetRequiredService<Franz.Common.Reflection.IAssemblyAccessor>().GetEntryAssembly());
        await pubChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            body: Encoding.UTF8.GetBytes("fail-me"),
            cancellationToken: cts.Token);
      }
      finally
      {
        channelPool.Return(pubChannel);
      }

      // Allow the consumer to process the published message before stopping it.
      await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
    }
    finally
    {
      await listener.StopListenAsync();
    }
  }

  [Fact]
  [Trait("Category", "FaultTolerance")]
  public async Task Listener_ShouldConfirmRetryPublicationBeforeAcknowledgingOriginalDelivery()
  {
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();
    var channelPool = _fixture.Services.GetRequiredService<IChannelPool>();
    var assemblyAccessor = _fixture.Services.GetRequiredService<IAssemblyAccessor>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    var body = $"retry-confirm-{Guid.NewGuid():N}";
    var retryProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var attempts = 0;

    listener.OnMessageReceivedAsync = args =>
    {
      if (!string.Equals(args.Message.Body, body, StringComparison.Ordinal))
        return Task.CompletedTask;

      if (Interlocked.Increment(ref attempts) == 1)
        throw new InvalidOperationException("Force one retry.");

      retryProcessed.TrySetResult();
      return Task.CompletedTask;
    };

    try
    {
      await listener.Listen(cts.Token);
      await listener.WaitUntilReadyAsync().WaitAsync(cts.Token);

      var queueName = QueueNamer.GetQueueName(
          assemblyAccessor.GetEntryAssembly()
          ?? throw new InvalidOperationException("Entry assembly cannot be null."));
      var publisher = await channelPool.GetAsync(cts.Token);
      try
      {
        await publisher.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            body: Encoding.UTF8.GetBytes(body),
            cancellationToken: cts.Token);
      }
      finally
      {
        channelPool.Return(publisher);
      }

      await retryProcessed.Task.WaitAsync(cts.Token);
      attempts.Should().Be(2);
      listener.Completion.IsCompleted.Should().BeFalse(
          "a failed handler followed by a confirmed retry must not close the consumer channel");
    }
    finally
    {
      listener.OnMessageReceivedAsync = null;
    }
  }

  [Fact]
  [Trait("Category", "FaultTolerance")]
  public async Task Listener_ShouldRouteRetryExhaustionToDeadLetterQueue()
  {
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();
    var channelPool = _fixture.Services.GetRequiredService<IChannelPool>();
    var assemblyAccessor = _fixture.Services.GetRequiredService<IAssemblyAccessor>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var body = $"retry-exhausted-{Guid.NewGuid():N}";
    var handlerFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    listener.OnMessageReceivedAsync = args =>
    {
      if (string.Equals(args.Message.Body, body, StringComparison.Ordinal))
      {
        handlerFailed.TrySetResult();
        throw new InvalidOperationException("Retry budget already exhausted.");
      }

      return Task.CompletedTask;
    };

    try
    {
      await listener.Listen(cts.Token);
      await listener.WaitUntilReadyAsync().WaitAsync(cts.Token);

      var assembly = assemblyAccessor.GetEntryAssembly()
          ?? throw new InvalidOperationException("Entry assembly cannot be null.");
      var queueName = QueueNamer.GetQueueName(assembly);
      var deadLetterQueue = QueueNamer.GetDeadLetterQueueName(assembly);
      var publisher = await channelPool.GetAsync(cts.Token);
      try
      {
        var properties = new BasicProperties
        {
          Headers = new Dictionary<string, object?> { ["x-replay"] = 3 }
        };
        await publisher.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(body),
            cancellationToken: cts.Token);
      }
      finally
      {
        channelPool.Return(publisher);
      }

      await handlerFailed.Task.WaitAsync(cts.Token);

      var deadLetterChannel = await channelPool.GetAsync(cts.Token);
      try
      {
        BasicGetResult? deadLetter = null;
        for (var attempt = 0; attempt < 50 && deadLetter is null; attempt++)
        {
          var candidate = await deadLetterChannel.BasicGetAsync(
              deadLetterQueue, autoAck: true, cancellationToken: cts.Token);
          if (candidate is not null &&
              string.Equals(Encoding.UTF8.GetString(candidate.Body.Span), body, StringComparison.Ordinal))
            deadLetter = candidate;
          else
            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
        }

        deadLetter.Should().NotBeNull("the exhausted delivery must be confirmed in the DLQ");
      }
      finally
      {
        channelPool.Return(deadLetterChannel);
      }

      listener.Completion.IsCompleted.Should().BeFalse(
          "routing a failed delivery to the DLQ must not close the consumer channel");
    }
    finally
    {
      listener.OnMessageReceivedAsync = null;
    }
  }

  [Fact]
  [Trait("Category", "Concurrency")]
  public async Task Listener_ShouldProcessUpToConfiguredConcurrencyInParallel()
  {
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();
    var channelPool = _fixture.Services.GetRequiredService<IChannelPool>();
    var assemblyAccessor = _fixture.Services.GetRequiredService<IAssemblyAccessor>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    var threeHandlersEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var releaseHandlers = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var activeHandlers = 0;
    var maximumConcurrency = 0;

    listener.OnMessageReceivedAsync = async _ =>
    {
      var active = Interlocked.Increment(ref activeHandlers);
      int observed;
      do
      {
        observed = Volatile.Read(ref maximumConcurrency);
        if (active <= observed)
          break;
      }
      while (Interlocked.CompareExchange(ref maximumConcurrency, active, observed) != observed);

      if (active >= 3)
        threeHandlersEntered.TrySetResult();

      try
      {
        await releaseHandlers.Task.WaitAsync(cts.Token).ConfigureAwait(false);
      }
      finally
      {
        Interlocked.Decrement(ref activeHandlers);
      }
    };

    try
    {
      await listener.Listen(cts.Token);
      await listener.WaitUntilReadyAsync().WaitAsync(cts.Token);

      var queueName = QueueNamer.GetQueueName(
          assemblyAccessor.GetEntryAssembly()
          ?? throw new InvalidOperationException("Entry assembly cannot be null."));
      var publisher = await channelPool.GetAsync(cts.Token);
      try
      {
        for (var index = 0; index < 3; index++)
        {
          await publisher.BasicPublishAsync(
              exchange: string.Empty,
              routingKey: queueName,
              body: Encoding.UTF8.GetBytes($"concurrency-{Guid.NewGuid():N}"),
              cancellationToken: cts.Token);
        }
      }
      finally
      {
        channelPool.Return(publisher);
      }

      await threeHandlersEntered.Task.WaitAsync(cts.Token);
      maximumConcurrency.Should().Be(3);
    }
    finally
    {
      releaseHandlers.TrySetResult();
      listener.OnMessageReceivedAsync = null;
    }
  }

  [Fact]
  [Trait("Category", "Concurrency")]
  public async Task ChannelPool_ShouldSupportMultipleConcurrentListeners()
  {
    var pool = _fixture.Services.GetRequiredService<IChannelPool>();
    var channels = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => pool.GetAsync().AsTask()));

    channels.Length.Should().Be(5);
    foreach (var channel in channels)
    {
      channel.Should().NotBeNull();
      channel.IsOpen.Should().BeTrue();
      pool.Return(channel);
    }
  }
}
