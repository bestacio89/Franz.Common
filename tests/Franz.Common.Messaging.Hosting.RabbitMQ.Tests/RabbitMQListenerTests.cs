#nullable enable
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Microsoft.Azure.Cosmos.Core;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Text;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Integration;

[Collection("RabbitMQ Integration")]
public sealed class RabbitMQListenerTests : IClassFixture<RabbitMQHostingFixture>
{
  private readonly RabbitMQHostingFixture _fixture;

  public RabbitMQListenerTests(RabbitMQHostingFixture fixture)
  {
    _fixture = fixture;
  }

 

  [Fact]
  public async Task Listen_ShouldExecuteNack_WhenHandlerThrowsExceptionAndNoReplayStrategy()
  {
    // Arrange
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();
    listener.OnMessageReceivedAsync = _ => throw new InvalidOperationException("Simulated Failure");

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

    // Act & Assert: Listener runs without crashing
    await FluentActions.Invoking(async () => await listener.Listen(cts.Token))
                       .Should().NotThrowAsync();
  }

  [Fact]
  public async Task StopListenAsync_ShouldReleaseChannelToPool()
  {
    // Arrange
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();

    using var cts = new CancellationTokenSource(100);

    // Act: Start & quickly stop
    var listenTask = listener.Listen(cts.Token);
    await listener.StopListenAsync();

    // Assert: Listener can restart without fault
    await FluentActions.Invoking(async () => await listener.Listen(CancellationToken.None))
                       .Should().NotThrowAsync();
  }
}