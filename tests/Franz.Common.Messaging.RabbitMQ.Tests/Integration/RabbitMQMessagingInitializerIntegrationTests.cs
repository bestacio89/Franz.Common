#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Replay;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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

  [Fact]
  public async Task InitializeAsync_ShouldVerifyCreatedTopologyIncludingConfiguredRetryStages()
  {
    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"verify-consumer-{suffix}";
    var options = new RabbitMQMessagingOptions
    {
      QueueName = queueName,
      ExchangeName = $"{queueName}-exchange",
      DeadLetterQueueName = $"{queueName}-dlq",
      DeadLetterExchangeName = $"{queueName}-dead-letter",
      ReplayStrategy = "ProgressiveFourStep",
      FailurePolicy = new MessageFailurePolicy(maxRetryAttempts: 5, retryBackoff: TimeSpan.FromSeconds(30))
    };

    await using (var createModelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>()))
    {
      var createOptions = Options.Create(options);
      var createAccessor = CreateAssemblyAccessor();
      await new RabbitMQMessagingInitializer(createModelProvider, createAccessor.Object, createOptions)
          .InitializeAsync();
    }

    options.ProvisioningMode = ProvisioningMode.Verify;
    await using var verifyModelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    var verifyAccessor = CreateAssemblyAccessor();

    await new RabbitMQMessagingInitializer(
        verifyModelProvider,
        verifyAccessor.Object,
        Options.Create(options)).InitializeAsync();

    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.ProvisioningVerification)
        .Should().BeTrue();
  }

  [Fact]
  public async Task InitializeAsync_ShouldApplyConfiguredQueueArguments()
  {
    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"bounded-consumer-{suffix}";
    var deadLetterExchange = $"{queueName}-dead-letter";
    var deadLetterQueue = $"{queueName}-dlq";
    var options = new RabbitMQMessagingOptions
    {
      QueueName = queueName,
      ExchangeName = $"{queueName}-exchange",
      DeadLetterQueueName = deadLetterQueue,
      DeadLetterExchangeName = deadLetterExchange,
      QueueType = RabbitMQQueueType.Classic,
      MessageTtlMilliseconds = 5_000,
      MaxLength = 100,
      MaxLengthBytes = 10_000,
      ReplayStrategy = "NoReplay"
    };

    await using var modelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    await new RabbitMQMessagingInitializer(
        modelProvider,
        CreateAssemblyAccessor().Object,
        Options.Create(options)).InitializeAsync();

    var channelPool = _fixture.ServiceProvider.GetRequiredService<IChannelPool>();
    var verificationChannel = await channelPool.GetAsync();
    try
    {
      var expectedArguments = new Dictionary<string, object?>
      {
        ["x-queue-type"] = "classic",
        ["x-message-ttl"] = 5_000,
        ["x-max-length"] = 100L,
        ["x-max-length-bytes"] = 10_000L,
        ["x-dead-letter-exchange"] = deadLetterExchange,
        ["x-dead-letter-routing-key"] = deadLetterQueue
      };

      await verificationChannel.QueueDeclareAsync(
          queueName,
          durable: true,
          exclusive: false,
          autoDelete: false,
          arguments: expectedArguments);

      RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.BoundedDestination)
          .Should().BeTrue();
      RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.DurableDelivery)
          .Should().BeTrue();
    }
    finally
    {
      channelPool.Return(verificationChannel);
    }

    var mismatchChannel = await channelPool.GetAsync();
    try
    {
      var mismatchedArguments = new Dictionary<string, object?>
      {
        ["x-queue-type"] = "classic",
        ["x-message-ttl"] = 5_000,
        ["x-max-length"] = 101L,
        ["x-max-length-bytes"] = 10_000L,
        ["x-dead-letter-exchange"] = deadLetterExchange,
        ["x-dead-letter-routing-key"] = deadLetterQueue
      };

      var act = async () => await mismatchChannel.QueueDeclareAsync(
          queueName,
          durable: true,
          exclusive: false,
          autoDelete: false,
          arguments: mismatchedArguments);

      await act.Should().ThrowAsync<global::RabbitMQ.Client.Exceptions.OperationInterruptedException>();
    }
    finally
    {
      channelPool.Return(mismatchChannel);
    }
  }

  [Fact]
  public async Task InitializeAsync_WhenProvisioningIsOff_ShouldNotAccessBroker()
  {
    var modelProvider = new Mock<IModelProvider>(MockBehavior.Strict);
    var options = Options.Create(new RabbitMQMessagingOptions
    {
      ProvisioningMode = ProvisioningMode.Off,
      QueueName = $"off-consumer-{Guid.CreateVersion7():N}"
    });

    var initializer = new RabbitMQMessagingInitializer(
        modelProvider.Object,
        CreateAssemblyAccessor().Object,
        options);

    await initializer.InitializeAsync();

    modelProvider.Verify(
        provider => provider.GetChannelAsync(It.IsAny<CancellationToken>()),
        Times.Never);
  }

  [Fact]
  public async Task ProgressiveReplay_ShouldPublishConfiguredRetryAndDeadLetterStages()
  {
    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"retry-consumer-{suffix}";
    var deadLetterQueue = $"{queueName}-dlq";
    var options = new RabbitMQMessagingOptions
    {
      QueueName = queueName,
      ExchangeName = $"{queueName}-exchange",
      DeadLetterQueueName = deadLetterQueue,
      DeadLetterExchangeName = $"{queueName}-dead-letter",
      ReplayStrategy = "ProgressiveFourStep",
      FailurePolicy = new MessageFailurePolicy(maxRetryAttempts: 5, retryBackoff: TimeSpan.FromSeconds(30))
    };

    await using var modelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    await new RabbitMQMessagingInitializer(
        modelProvider,
        CreateAssemblyAccessor().Object,
        Options.Create(options)).InitializeAsync();

    var channelPool = _fixture.ServiceProvider.GetRequiredService<IChannelPool>();
    var strategy = new ProgressiveFourStepReplayStrategy(
        (IConfirmChannelPool)channelPool,
        CreateAssemblyAccessor().Object,
        Options.Create(options));

    var retryFive = new BasicDeliverEventArgs(
        "consumer",
        1,
        false,
        $"{queueName}-exchange",
        queueName,
        new BasicProperties(),
        new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes("retry-payload")));

    retryFive.BasicProperties.Headers = new Dictionary<string, object?>
    {
      [ProgressiveFourStepReplayStrategy.ReplayHeader] = 4
    };

    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.Retry).Should().BeTrue();
    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.DeadLetter).Should().BeTrue();
    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.DeliveryAttemptLimit).Should().BeTrue();
    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.PublisherAcknowledgements).Should().BeTrue();

    await strategy.ReplayAsync(retryFive, new InvalidOperationException("retry failure"));

    var verificationChannel = await channelPool.GetAsync();
    try
    {
      var retryQueue = await verificationChannel.QueueDeclarePassiveAsync($"{queueName}-retry-5");
      retryQueue.MessageCount.Should().Be(1);
    }
    finally
    {
      channelPool.Return(verificationChannel);
    }

    var exhausted = new BasicDeliverEventArgs(
        "consumer",
        2,
        false,
        $"{queueName}-exchange",
        queueName,
        new BasicProperties(),
        new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes("dead-letter-payload")));
    exhausted.BasicProperties.Headers = new Dictionary<string, object?>
    {
      [ProgressiveFourStepReplayStrategy.ReplayHeader] = 5
    };

    await strategy.ReplayAsync(exhausted, new InvalidOperationException("retry exhausted"));

    verificationChannel = await channelPool.GetAsync();
    try
    {
      var deadLetter = await verificationChannel.QueueDeclarePassiveAsync(deadLetterQueue);
      deadLetter.MessageCount.Should().Be(1);
    }
    finally
    {
      channelPool.Return(verificationChannel);
    }
  }

  [Fact]
  public async Task RabbitMQTransaction_ShouldCommitAndRollbackPublishedMessages()
  {
    var queueName = $"transaction-queue-{Guid.CreateVersion7():N}";
    var connectionProvider = _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>();
    var connection = await connectionProvider.GetConnectionAsync();
    await using var publishingChannel = await connection.CreateChannelAsync();
    await publishingChannel.QueueDeclareAsync(
        queueName,
        durable: false,
        exclusive: false,
        autoDelete: true);

    var transaction = new RabbitMQMessagingTransaction();
    transaction.Attach(publishingChannel);

    await transaction.BeginAsync();
    await publishingChannel.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: queueName,
        mandatory: false,
        basicProperties: new BasicProperties(),
        body: new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes("committed")));
    await transaction.CompleteAsync();

    await using var consumingChannel = await connection.CreateChannelAsync();
    var committed = await consumingChannel.BasicGetAsync(queueName, autoAck: true);
    committed.Should().NotBeNull();
    System.Text.Encoding.UTF8.GetString(committed!.Body.Span).Should().Be("committed");

    await transaction.BeginAsync();
    await publishingChannel.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: queueName,
        mandatory: false,
        basicProperties: new BasicProperties(),
        body: new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes("rolled-back")));
    await transaction.RollbackAsync();

    var rolledBack = await consumingChannel.BasicGetAsync(queueName, autoAck: true);
    rolledBack.Should().BeNull("a rolled-back publication must not become visible to consumers");

    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.TransactionalPublishing)
        .Should().BeTrue();
  }

  private static Mock<IAssemblyAccessor> CreateAssemblyAccessor()
  {
    var accessor = new Mock<IAssemblyAccessor>();
    accessor.Setup(value => value.GetEntryAssembly())
        .Returns(new AssemblyWrapper(typeof(RabbitMQMessagingInitializer).Assembly));
    return accessor;
  }

  private sealed class TestIntegrationEvent;
}
