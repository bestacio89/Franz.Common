#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
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
  public async Task Listen_ShouldStopCleanly_WhenCancellationIsRequested()
  {
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    await listener.Listen(cts.Token);
    await listener.WaitUntilReadyAsync().WaitAsync(cts.Token);

    await FluentActions.Invoking(() => listener.StopListenAsync())
                       .Should().NotThrowAsync();
  }

  [Fact]
  public async Task StopListenAsync_ShouldReleaseChannelToPool_AndAllowRestart()
  {
    var listener = _fixture.Services.GetRequiredService<RabbitMQListener>();
    using var firstCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    await listener.Listen(firstCts.Token);
    await listener.WaitUntilReadyAsync().WaitAsync(firstCts.Token);
    await listener.StopListenAsync();

    using var secondCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await listener.Listen(secondCts.Token);

    try
    {
      await FluentActions.Invoking(() => listener.StopListenAsync())
                         .Should().NotThrowAsync();
    }
    finally
    {
      // Ensure no background consumer survives the test or fixture teardown.
      await listener.StopListenAsync();
    }
  }
}
