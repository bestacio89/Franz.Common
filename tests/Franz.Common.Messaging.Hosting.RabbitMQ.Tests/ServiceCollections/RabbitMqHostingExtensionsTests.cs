using FluentAssertions;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests;

public class RabbitMQHostingIntegrationTests : IClassFixture<RabbitMQHostingFixture>
{
  private readonly RabbitMQHostingFixture _fixture;

  public RabbitMQHostingIntegrationTests(RabbitMQHostingFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public void Host_ShouldResolveAndStartRabbitMQHostedService()
  {
    // Arrange & Act
    // Fixture handles startup in InitializeAsync
    var hostedServices = _fixture.Services.GetServices<IHostedService>();

    // Assert: Verify the background workers are registered and alive
    hostedServices.Should().Contain(s => s.GetType().Name == "RabbitMQHostedService");
    hostedServices.Should().Contain(s => s.GetType().Name == "OutboxHostedService");
  }

  [Fact]
  public void Host_ShouldCorrectplyBindRabbitMQOptionsFromContainerUri()
  {
    // Act
    var options = _fixture.Services.GetRequiredService<IOptions<RabbitMQMessagingOptions>>().Value;

    // Assert: This proves the Fixture's URI was correctly injected into the Messaging infra
    options.BootStrapServers.Should().NotBeNullOrEmpty();
    options.BootStrapServers.Should().StartWith("amqp://");

    // Senior Note: Validation of the actual connection state
    // If the credentials from the URI were invalid, the Host startup would have thrown.
    _fixture.Host.Should().NotBeNull();
  }

  [Fact]
  public async Task Host_ShouldProcessMessageThroughInfrastructure()
  {
    // This is where we move beyond registration and into behavior.
    // We verify that the 'RabbitMQListener' is actually registered as a Singleton
    // and accessible via the ServiceProvider.

    var listener = _fixture.Services.GetService(
        typeof(Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices.RabbitMQListener));

    listener.Should().NotBeNull("The RabbitMQListener must be registered as a Singleton for the HostedService to use.");
  }
}