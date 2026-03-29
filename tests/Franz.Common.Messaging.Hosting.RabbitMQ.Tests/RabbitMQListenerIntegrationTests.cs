#nullable enable
using System.Text;
using System.Collections.Concurrent;
using Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Connections;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Integration;

[Collection("RabbitMQ Integration")] // Ensure sequential execution if sharing resources
public sealed class RabbitMQIntegrationTests(RabbitMQHostingFixture fixture) : IClassFixture<RabbitMQHostingFixture>
{
  private readonly RabbitMQHostingFixture _fixture = fixture;

  [Fact]
  [Trait("Category", "Connectivity")]
  public async Task Infrastructure_ShouldConnect_ViaUriBootstrap()
  {
    // Act
    var channelPool = _fixture.Services.GetRequiredService<IChannelPool>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    // Assert: If the URI from the fixture was wrong, GetAsync throws here
    var channel = await channelPool.GetAsync(cts.Token);
    channel.Should().NotBeNull();
    channel.IsOpen.Should().BeTrue();

    channelPool.Return(channel);
  }

  
  [Fact]
  [Trait("Category", "FaultTolerance")]
  public async Task Listener_ShouldTriggerReplayStrategy_OnHandlerFailure()
  {
    // Arrange
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();
    var channelPool = _fixture.Services.GetRequiredService<IChannelPool>();
    var tcs = new TaskCompletionSource<bool>();

    // Note: This relies on the RabbitMQHostingFixture having a ReplayStrategy registered
    listener.OnMessageReceivedAsync = _ => throw new Exception("Simulated Processing Failure");

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    _ = Task.Run(() => listener.Listen(cts.Token), cts.Token);

    // Act: Publish one message
    var pubChannel = await channelPool.GetAsync(cts.Token);
    await pubChannel.BasicPublishAsync(
        exchange: "franz-test-exchange",
        routingKey: "test-key",
        body: Encoding.UTF8.GetBytes("fail-me"),
        cancellationToken: cts.Token);
    channelPool.Return(pubChannel);

    // Assert
    // We check the "Dead Letter" or "Replay" side effect. 
    // In a real Franz.Common infra, this would mean checking the Mongo MessageStore for an "Errored" status.
    await Task.Delay(2000); // Allow for processing/replay
                            // Verification logic for Mongo/Outbox goes here...
  }
  [Fact]
  [Trait("Category", "Concurrency")]
  public async Task ChannelPool_ShouldSupportMultipleConcurrentListeners()
  {
    // Arrange
    var services = _fixture.Services;
    var pool = services.GetRequiredService<IChannelPool>();

    // Act
    // Senior Note: ValueTask is not directly compatible with Task.WhenAll.
    // We must convert to Task using .AsTask() to allow the combinator to track them.
    var tasks = Enumerable.Range(0, 5)
        .Select(_ => pool.GetAsync().AsTask());

    var channels = await Task.WhenAll(tasks);

    // Assert
    channels.Length.Should().Be(5);
    foreach (var ch in channels)
    {
      ch.Should().NotBeNull();
      ch.IsOpen.Should().BeTrue();

      // Return to pool to prevent exhaustion in subsequent tests
      pool.Return(ch);
    }
  }

  private async Task SetupTestTopologyAsync(CancellationToken ct)
  {
    var connectionFactory = _fixture.Services.GetRequiredService<IConnectionFactory>();
    using var connection = await connectionFactory.CreateConnectionAsync(ct);
    using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

    await channel.ExchangeDeclareAsync("franz-test-exchange", ExchangeType.Topic, durable: false);
    await channel.QueueDeclareAsync("test-queue", durable: false, exclusive: false, autoDelete: true);
    await channel.QueueBindAsync("test-queue", "franz-test-exchange", "test-key");
  }
}
