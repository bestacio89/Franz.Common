#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Manifest;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;

[Collection(nameof(RabbitMqTestCollection))]
public sealed class RabbitMQMessagingInitializerIntegrationTests
{
  private readonly RabbitMqContainerFixture _fixture;

  public RabbitMQMessagingInitializerIntegrationTests(RabbitMqContainerFixture fixture)
      => _fixture = fixture;

  [Fact]
  public async Task InitializeAsync_ShouldProvisionDestinationExchangeFromManifestAssembly()
  {
    var eventAssembly = typeof(TestIntegrationEvent).Assembly;
    MessagingManifestRegistry.Register(
        eventAssembly,
        new MessagingManifest(destinations: new[] { typeof(TestIntegrationEvent) }));

    // Use a real assembly wrapper so both Name and FullName satisfy the IAssembly contract.
    // The event contract assembly remains distinct from the consuming service assembly.
    var entryAssembly = new AssemblyWrapper(typeof(RabbitMQMessagingInitializer).Assembly);

    var accessor = new Mock<IAssemblyAccessor>();
    accessor.Setup(value => value.GetEntryAssembly()).Returns(entryAssembly);

    var queueSuffix = Guid.CreateVersion7().ToString("N");
    var options = Options.Create(new RabbitMQMessagingOptions
    {
      QueueName = $"manifest-consumer-{queueSuffix}",
      DeadLetterQueueName = $"manifest-consumer-{queueSuffix}-dlq",
      DeadLetterExchangeName = $"manifest-consumer-{queueSuffix}-dead-letter",
      ReplayStrategy = "NoReplay"
    });

    await using var modelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    var initializer = new RabbitMQMessagingInitializer(modelProvider, accessor.Object, options);

    await initializer.InitializeAsync();

    var channelPool = _fixture.ServiceProvider.GetRequiredService<IChannelPool>();
    var verificationChannel = await channelPool.GetAsync();
    try
    {
      var destinationExchange = RabbitMQExchangeNamer.GetEventExchangeName(eventAssembly);
      await verificationChannel.ExchangeDeclarePassiveAsync(destinationExchange);
    }
    finally
    {
      channelPool.Return(verificationChannel);
    }
  }

  private sealed class TestIntegrationEvent;
}
