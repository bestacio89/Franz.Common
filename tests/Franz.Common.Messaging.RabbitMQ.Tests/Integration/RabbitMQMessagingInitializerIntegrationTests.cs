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
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DotNet.Testcontainers.Builders;
using Testcontainers.RabbitMq;

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
      ReplayStrategy = RabbitMQReplayStrategy.NoReplay
    });

    await using var modelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    var initializer = new RabbitMQMessagingInitializer(modelProvider, _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(), accessor.Object, options);

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
      ManagementApiUrl = _fixture.ManagementApiUrl,
      ReplayStrategy = RabbitMQReplayStrategy.ProgressiveFourStep,
      FailurePolicy = new MessageFailurePolicy(maxRetryAttempts: 5, retryBackoff: TimeSpan.FromSeconds(30))
    };

    await using (var createModelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>()))
    {
      var createOptions = Options.Create(options);
      var createAccessor = CreateAssemblyAccessor();
      await new RabbitMQMessagingInitializer(createModelProvider, _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(), createAccessor.Object, createOptions)
          .InitializeAsync();
    }

    options.ProvisioningMode = ProvisioningMode.Verify;
    await using var verifyModelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    var verifyAccessor = CreateAssemblyAccessor();

    await new RabbitMQMessagingInitializer(
        verifyModelProvider,
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
        verifyAccessor.Object,
        Options.Create(options)).InitializeAsync();

    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.ProvisioningVerification)
        .Should().BeTrue();
  }

  [Fact]
  public async Task VerifyMode_ShouldRejectMissingDeadLetterBinding()
  {
    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"verify-binding-{suffix}";
    var deadLetterExchange = $"{queueName}-dead-letter";
    var deadLetterQueue = $"{queueName}-dlq";
    var options = new RabbitMQMessagingOptions
    {
      QueueName = queueName,
      ExchangeName = $"{queueName}-exchange",
      DeadLetterExchangeName = deadLetterExchange,
      DeadLetterQueueName = deadLetterQueue,
      ManagementApiUrl = _fixture.ManagementApiUrl,
      ReplayStrategy = RabbitMQReplayStrategy.NoReplay
    };

    await using (var createModelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>()))
    {
      await new RabbitMQMessagingInitializer(
          createModelProvider,
          _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
          CreateAssemblyAccessor().Object,
          Options.Create(options)).InitializeAsync();
    }

    var connection = await _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>().GetConnectionAsync();
    await using (var channel = await connection.CreateChannelAsync())
    {
      await channel.QueueUnbindAsync(
          queue: deadLetterQueue,
          exchange: deadLetterExchange,
          routingKey: deadLetterQueue);
    }

    options.ProvisioningMode = ProvisioningMode.Verify;
    await using var verifyModelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    var verify = new RabbitMQMessagingInitializer(
        verifyModelProvider,
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
        CreateAssemblyAccessor().Object,
        Options.Create(options));

    var act = () => verify.InitializeAsync().AsTask();

    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*required binding*");
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
      ReplayStrategy = RabbitMQReplayStrategy.NoReplay
    };

    await using var modelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    await new RabbitMQMessagingInitializer(
        modelProvider,
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
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
  public async Task VerifyMode_ShouldAggregateMissingTopologyAndReportQueueArgumentDrift()
  {
    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"verify-drift-{suffix}";
    var exchangeName = $"{queueName}-exchange";
    var deadLetterExchange = $"{queueName}-dead-letter";
    var deadLetterQueue = $"{queueName}-dlq";

    var connection = await _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>().GetConnectionAsync();
    await using (var legacyChannel = await connection.CreateChannelAsync())
    {
      await legacyChannel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
          arguments: new Dictionary<string, object?> { ["x-queue-type"] = "classic" });
    }

    var options = Options.Create(new RabbitMQMessagingOptions
    {
      ProvisioningMode = ProvisioningMode.Verify,
      QueueName = queueName,
      ExchangeName = exchangeName,
      DeadLetterExchangeName = deadLetterExchange,
      DeadLetterQueueName = deadLetterQueue,
      QueueType = RabbitMQQueueType.Quorum
    });

    await using var modelProvider = new ModelProvider(_fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    var initializer = new RabbitMQMessagingInitializer(
        modelProvider,
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
        CreateAssemblyAccessor().Object,
        options);

    var act = () => initializer.InitializeAsync().AsTask();

    var error = await act.Should().ThrowAsync<InvalidOperationException>();
    error.Which.Message.Should().Contain(queueName)
        .And.Contain(exchangeName)
        .And.Contain(deadLetterExchange)
        .And.Contain(deadLetterQueue);
  }

  [Fact]
  public async Task CreateMode_ShouldExplainIncompatibleQueueCreatedByVersion234()
  {
    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"legacy-234-{suffix}";
    var deadLetterExchange = $"{queueName}-dead-letter";
    var deadLetterQueue = $"{queueName}-dlq";
    var connection = await _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>().GetConnectionAsync();

    // Version 2.3.4 declared the dead-letter arguments but relied on the broker's
    // default queue type. The default RabbitMQ 4.0 broker uses classic.
    await using (var legacyChannel = await connection.CreateChannelAsync())
    {
      await legacyChannel.QueueDeclareAsync(
          queueName,
          durable: true,
          exclusive: false,
          autoDelete: false,
          arguments: new Dictionary<string, object?>
          {
            ["x-dead-letter-exchange"] = deadLetterExchange,
            ["x-dead-letter-routing-key"] = deadLetterQueue
          });
    }

    var options = Options.Create(new RabbitMQMessagingOptions
    {
      QueueName = queueName,
      ExchangeName = $"{queueName}-exchange",
      DeadLetterExchangeName = deadLetterExchange,
      DeadLetterQueueName = deadLetterQueue,
      QueueType = RabbitMQQueueType.Quorum
    });

    await using var modelProvider = new ModelProvider(_fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    var initializer = new RabbitMQMessagingInitializer(
        modelProvider,
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
        CreateAssemblyAccessor().Object,
        options);

    var act = () => initializer.InitializeAsync().AsTask();

    var error = await act.Should().ThrowAsync<InvalidOperationException>();
    error.Which.Message.Should().Contain(queueName)
        .And.Contain("incompatible");
  }

  [Fact]
  public async Task CreateMode_ShouldMatchExplicitQueueTypeWithBrokerDefaultQuorum()
  {
    await using var container = new RabbitMqBuilder(RabbitMqContainerFixture.Image)
        .WithCleanUp(true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilInternalTcpPortIsAvailable(5672)
            .UntilCommandIsCompleted("rabbitmq-diagnostics -q ping"))
        .Build();
    await container.StartAsync();

    // The default vhost metadata takes precedence over the node-wide setting.
    // Configure the broker's effective default explicitly so this test verifies
    // declarations that omit x-queue-type against a known quorum default.
    var defaultQueueType = await container.ExecAsync(
        ["rabbitmqctl", "update_vhost_metadata", "/", "--default-queue-type", "quorum"]);
    defaultQueueType.ExitCode.Should().Be(0,
        $"RabbitMQ must accept the quorum default queue type. stdout: {defaultQueueType.Stdout}; stderr: {defaultQueueType.Stderr}");

    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"broker-default-quorum-{suffix}";
    var deadLetterExchange = $"{queueName}-dead-letter";
    var deadLetterQueue = $"{queueName}-dlq";
    var options = Options.Create(new RabbitMQMessagingOptions
    {
      BootStrapServers = container.GetConnectionString(),
      QueueName = queueName,
      ExchangeName = $"{queueName}-exchange",
      DeadLetterExchangeName = deadLetterExchange,
      DeadLetterQueueName = deadLetterQueue,
      QueueType = RabbitMQQueueType.Quorum,
      ReplayStrategy = RabbitMQReplayStrategy.NoReplay
    });

    var services = new ServiceCollection();
    services.AddSingleton(options);
    services.AddSingleton<IAssemblyAccessor, AssemblyAccessorWrapper>();
    services.AddSingleton<IConnectionFactoryProvider, ConnectionFactoryProvider>();
    services.AddSingleton<IConnectionProvider, ConnectionProvider>();
    await using var serviceProvider = services.BuildServiceProvider();
    var connectionProvider = serviceProvider.GetRequiredService<IConnectionProvider>();

    var connection = await connectionProvider.GetConnectionAsync();
    await using (var legacyChannel = await connection.CreateChannelAsync())
    {
      // Simulate a queue declared without x-queue-type on a quorum-default broker.
      // Its other immutable arguments match the initializer's expected topology.
      await legacyChannel.QueueDeclareAsync(
          queueName,
          durable: true,
          exclusive: false,
          autoDelete: false,
          arguments: new Dictionary<string, object?>
          {
            ["x-dead-letter-exchange"] = deadLetterExchange,
            ["x-dead-letter-routing-key"] = deadLetterQueue
          });
    }

    await using var modelProvider = new ModelProvider(connectionProvider);
    var initializer = new RabbitMQMessagingInitializer(
        modelProvider,
        connectionProvider,
        serviceProvider.GetRequiredService<IAssemblyAccessor>(),
        options);

    await initializer.InitializeAsync();
  }

  [Fact]
  public async Task CreateMode_ShouldPreserveMatchingLegacyQuorumTopology()
  {
    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"legacy-quorum-{suffix}";
    var exchangeName = $"{queueName}-exchange";
    var deadLetterExchange = $"{queueName}-dead-letter";
    var deadLetterQueue = $"{queueName}-dlq";

    var connection = await _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>().GetConnectionAsync();
    await using (var legacyChannel = await connection.CreateChannelAsync())
    {
      await legacyChannel.ExchangeDeclareAsync(exchangeName, ExchangeType.Headers, durable: true);
      await legacyChannel.ExchangeDeclareAsync(deadLetterExchange, ExchangeType.Direct, durable: true);
      await legacyChannel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
          arguments: new Dictionary<string, object?>
          {
            ["x-queue-type"] = "quorum",
            ["x-dead-letter-exchange"] = deadLetterExchange,
            ["x-dead-letter-routing-key"] = deadLetterQueue
          });
      await legacyChannel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false,
          arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" });
    }

    var options = Options.Create(new RabbitMQMessagingOptions
    {
      QueueName = queueName,
      ExchangeName = exchangeName,
      DeadLetterExchangeName = deadLetterExchange,
      DeadLetterQueueName = deadLetterQueue,
      QueueType = RabbitMQQueueType.Quorum,
      ReplayStrategy = RabbitMQReplayStrategy.NoReplay
    });

    await using var modelProvider = new ModelProvider(_fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    var initializer = new RabbitMQMessagingInitializer(
        modelProvider,
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
        CreateAssemblyAccessor().Object,
        options);

    await initializer.InitializeAsync();
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
        Mock.Of<IConnectionProvider>(),
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
      ReplayStrategy = RabbitMQReplayStrategy.ProgressiveFourStep,
      FailurePolicy = new MessageFailurePolicy(maxRetryAttempts: 5, retryBackoff: TimeSpan.FromSeconds(30))
    };

    await using var modelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    await new RabbitMQMessagingInitializer(
        modelProvider,
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
        CreateAssemblyAccessor().Object,
        Options.Create(options)).InitializeAsync();

    var channelPool = _fixture.ServiceProvider.GetRequiredService<IChannelPool>();
    var strategy = new ProgressiveFourStepReplayStrategy(
        (IConfirmChannelPool)channelPool,
        CreateAssemblyAccessor().Object,
        Options.Create(options));

    var retryProperties = new BasicProperties
    {
      Headers = new Dictionary<string, object?>
      {
        [ProgressiveFourStepReplayStrategy.ReplayHeader] = 4
      }
    };
    var retryFive = new BasicDeliverEventArgs(
        "consumer",
        1,
        false,
        $"{queueName}-exchange",
        queueName,
        retryProperties,
        new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes("retry-payload")));

    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.Retry).Should().BeTrue();
    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.DeadLetter).Should().BeTrue();
    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.DeliveryAttemptLimit).Should().BeTrue();
    RabbitMQMessagingCapabilities.Descriptor.Supports(MessagingCapability.PublisherAcknowledgements).Should().BeTrue();

    await strategy.ReplayAsync(retryFive, new InvalidOperationException("retry failure"));

    var verificationChannel = await channelPool.GetAsync();
    try
    {
      var retryQueue = await verificationChannel.QueueDeclarePassiveAsync($"{queueName}-retry-3");
      retryQueue.MessageCount.Should().Be(1);
    }
    finally
    {
      channelPool.Return(verificationChannel);
    }

    var exhaustedProperties = new BasicProperties
    {
      Headers = new Dictionary<string, object?>
      {
        [ProgressiveFourStepReplayStrategy.ReplayHeader] = 5
      }
    };
    var exhausted = new BasicDeliverEventArgs(
        "consumer",
        2,
        false,
        $"{queueName}-exchange",
        queueName,
        exhaustedProperties,
        new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes("dead-letter-payload")));

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
  public async Task ProgressiveReplay_ShouldSurfaceDeadLetterPublicationFailure()
  {
    var suffix = Guid.CreateVersion7().ToString("N");
    var queueName = $"failed-dlq-consumer-{suffix}";
    var options = new RabbitMQMessagingOptions
    {
      QueueName = queueName,
      ExchangeName = $"{queueName}-exchange",
      DeadLetterQueueName = $"{queueName}-dlq",
      DeadLetterExchangeName = $"{queueName}-dead-letter",
      ReplayStrategy = RabbitMQReplayStrategy.NoReplay
    };

    await using var modelProvider = new ModelProvider(
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>());
    await new RabbitMQMessagingInitializer(
        modelProvider,
        _fixture.ServiceProvider.GetRequiredService<IConnectionProvider>(),
        CreateAssemblyAccessor().Object,
        Options.Create(options)).InitializeAsync();

    // Deliberately point the replay strategy at a destination that does not
    // exist. Mandatory publishing with confirms must surface the unroutable
    // dead-letter instead of allowing the listener to acknowledge the source.
    var failureOptions = new RabbitMQMessagingOptions
    {
      QueueName = queueName,
      DeadLetterQueueName = $"{queueName}-missing-dlq",
      FailurePolicy = new MessageFailurePolicy(maxRetryAttempts: 0)
    };
    var strategy = new ProgressiveFourStepReplayStrategy(
        (IConfirmChannelPool)_fixture.ServiceProvider.GetRequiredService<IChannelPool>(),
        CreateAssemblyAccessor().Object,
        Options.Create(failureOptions));
    var delivery = new BasicDeliverEventArgs(
        "consumer",
        3,
        false,
        $"{queueName}-exchange",
        queueName,
        new BasicProperties(),
        new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes("unroutable")));

    var act = () => strategy.ReplayAsync(delivery, new InvalidOperationException("exhausted"));

    await act.Should().ThrowAsync<Exception>();
  }

  [Fact]
  public async Task ProgressiveReplay_WhenDeadLetteringIsDisabled_ShouldNotPublish()
  {
    var confirmChannelPool = new Mock<IConfirmChannelPool>(MockBehavior.Strict);
    var options = Options.Create(new RabbitMQMessagingOptions
    {
      QueueName = $"disabled-dlq-{Guid.CreateVersion7():N}",
      FailurePolicy = new MessageFailurePolicy(
          maxRetryAttempts: 0,
          deadLetterOnExhaustion: false)
    });
    var strategy = new ProgressiveFourStepReplayStrategy(
        confirmChannelPool.Object,
        CreateAssemblyAccessor().Object,
        options);
    var delivery = new BasicDeliverEventArgs(
        "consumer",
        4,
        false,
        string.Empty,
        options.Value.QueueName!,
        new BasicProperties(),
        ReadOnlyMemory<byte>.Empty);

    await strategy.ReplayAsync(delivery, new InvalidOperationException("exhausted"));

    confirmChannelPool.Verify(
        pool => pool.GetPublisherConfirmChannelAsync(It.IsAny<CancellationToken>()),
        Times.Never);
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

    await using var rollbackChannel = await connection.CreateChannelAsync();
    var rollbackTransaction = new RabbitMQMessagingTransaction();
    rollbackTransaction.Attach(rollbackChannel);
    await rollbackTransaction.BeginAsync();
    await rollbackChannel.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: queueName,
        mandatory: false,
        basicProperties: new BasicProperties(),
        body: new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes("rolled-back")));
    await rollbackTransaction.RollbackAsync();

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
