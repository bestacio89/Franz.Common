#nullable enable
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;
using Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fixtures;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Mediator.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Hosting;

public class MessagingPublisherTests
    : IClassFixture<RabbitMqContainerFixture>
{
  private readonly RabbitMqContainerFixture _fixture;

  public MessagingPublisherTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;
  }

  [Fact]
  public async Task MessagingPublisher_invokes_IMessageHandler()
  {
    // Reset the static state of the fake
    TestMessageHandler.LastMessage = null;

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:HostName"] = _fixture.Host,
          ["Messaging:Port"] = _fixture.Port.ToString(),
          ["Messaging:UserName"] = "guest",
          ["Messaging:Password"] = "guest"
        })
        .Build();

    using var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
          services.AddLogging();

          // 🔑 Infrastructure setup
          services.AddRabbitMQMessaging(configuration);
          services.AddFranzMediator(new[] { typeof(TestIntegrationEvent).Assembly });

          // Serialization
          services.AddMessagingSerialization();

          // Register our fake handler which now uses native Guids
          services.AddSingleton<IMessageHandler, TestMessageHandler>();
        })
        .Build();

    await host.StartAsync();

    var publisher = host.Services.GetRequiredService<IMessagingPublisher>();

    // BAZOOKA REFACTOR: Passing a Guid into the event
    await publisher.Publish(new TestIntegrationEvent(Guid.CreateVersion7()));

    // Give the async consumer a moment if necessary (or rely on the test loop)
    // In some RabbitMQ tests, you might need a small delay here.

    Assert.NotNull(TestMessageHandler.LastMessage);

    // ✅ FIX: Compare the Guid property against the static Guid defined in the Fake.
    // This confirms the "Spine" was successfully mutated and preserved through RabbitMQ.
    Assert.Equal(
        TestMessageHandler.TestCorrelationId,
        TestMessageHandler.LastMessage!.CorrelationId);

    await host.StopAsync();
  }
}