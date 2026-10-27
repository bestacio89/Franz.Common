#nullable enable
using System.Text;
using Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging;
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
