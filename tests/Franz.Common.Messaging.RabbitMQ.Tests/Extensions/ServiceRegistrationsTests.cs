#nullable enable
using FluentAssertions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Franz.Common.Messaging.RabbitMQ.Extensions;
using Franz.Common.Messaging.RabbitMQ.Modeling;
using Franz.Common.Messaging.RabbitMQ.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;

[Collection(nameof(RabbitMqTestCollection))]
public sealed class ServiceRegistrationTests
{
  private readonly IServiceCollection _services = new ServiceCollection();
  private readonly IConfiguration _configuration;
  private readonly RabbitMqContainerFixture _fixture;

  public ServiceRegistrationTests(RabbitMqContainerFixture fixture)
  {
    _fixture = fixture;

    _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:RabbitMQ:BootStrapServers"] = _fixture.ConnectionString,
          ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5"
        })
        .Build();
  }

  [Fact]
  public async Task AddRabbitMQMessaging_ShouldProvideFunctionalConnectionProvider()
  {
    // Arrange
    _services.AddRabbitMQMessaging(_configuration);
    var provider = _services.BuildServiceProvider();

    // Assert DI registration
    _services.Should().ContainSingle(s =>
        s.ServiceType == typeof(IConnectionProvider) &&
        s.Lifetime == ServiceLifetime.Singleton);

    // Act
    var connectionProvider = provider.GetRequiredService<IConnectionProvider>();
    using var connection = await connectionProvider.GetConnectionAsync();

    // Assert connection
    connection.Should().NotBeNull();
    connection.IsOpen.Should().BeTrue();
  }

  [Fact]
  public void AddRabbitMQMessaging_ShouldRegisterCoreInfrastructureAsSingletons()
  {
    _services.AddRabbitMQMessaging(_configuration);

    _services.Should().ContainSingle(s =>
        s.ServiceType == typeof(IConnectionProvider) &&
        s.Lifetime == ServiceLifetime.Singleton);

    _services.Should().ContainSingle(s =>
        s.ServiceType == typeof(IChannelPool) &&
        s.Lifetime == ServiceLifetime.Singleton);

    _services.Should().ContainSingle(s =>
        s.ServiceType == typeof(IModelProvider) &&
        s.Lifetime == ServiceLifetime.Singleton);
  }

  [Fact]
  public async Task AddRabbitMQMessaging_ShouldRegisterPublisherAndTransactionAsScoped()
  {
    // Arrange
    _services.AddRabbitMQMessaging(_configuration);
    var provider = _services.BuildServiceProvider();

    // Act & Assert
    // FIX: Use 'await using' to allow the container to call DisposeAsync()
    await using (var scope = provider.CreateAsyncScope())
    {
      var publisher = scope.ServiceProvider.GetRequiredService<IMessagingPublisher>();
      publisher.Should().BeOfType<RabbitMQMessagingPublisher>();

      var transaction = scope.ServiceProvider.GetRequiredService<IMessagingTransaction>();
      transaction.Should().NotBeNull();
    } // Async disposal happens here automatically
  }

  [Fact]
  public void AddRabbitMQMessaging_ShouldConfigureOptionsFromContainerFixture()
  {
    _services.AddRabbitMQMessaging(_configuration);
    var provider = _services.BuildServiceProvider();

    var options = provider.GetRequiredService<IOptions<RabbitMQMessagingOptions>>().Value;
    options.BootStrapServers.Should().Be(_fixture.ConnectionString);
  }

  [Fact]
  public async Task AddRabbitMQMessaging_ShouldValidateMissingConnectionSettingsAtHostStartup()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5"
        })
        .Build();

    using var host = new HostBuilder()
        .ConfigureServices((_, services) => services.AddRabbitMQMessaging(configuration))
        .Build();

    var act = () => host.StartAsync();

    await act.Should().ThrowAsync<OptionsValidationException>()
        .WithMessage("*BootStrapServers*");
  }

  [Fact]
  public async Task AddRabbitMQMessaging_ShouldRejectUriWithoutExplicitCredentialsAtHostStartup()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:RabbitMQ:BootStrapServers"] = "amqp://localhost:5672/",
          ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5"
        })
        .Build();

    using var host = new HostBuilder()
        .ConfigureServices((_, services) => services.AddRabbitMQMessaging(configuration))
        .Build();

    var act = () => host.StartAsync();

    await act.Should().ThrowAsync<OptionsValidationException>()
        .WithMessage("*explicit username/password credentials*");
  }

  [Fact]
  public async Task AddRabbitMQMessaging_ShouldRejectTlsWithNonTlsUriAtHostStartup()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:RabbitMQ:BootStrapServers"] = "amqp://guest:guest@localhost:5672/",
          ["Messaging:RabbitMQ:SslEnabled"] = "true",
          ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5"
        })
        .Build();

    using var host = new HostBuilder()
        .ConfigureServices((_, services) => services.AddRabbitMQMessaging(configuration))
        .Build();

    var act = () => host.StartAsync();

    await act.Should().ThrowAsync<OptionsValidationException>()
        .WithMessage("*does not use amqps://*");
  }

  [Theory]
  [InlineData("HostName")]
  [InlineData("UserName")]
  [InlineData("Password")]
  public async Task AddRabbitMQMessaging_ShouldRejectIncompleteExplicitConnectionSettingsAtHostStartup(string missingSetting)
  {
    var settings = new Dictionary<string, string?>
    {
      ["Messaging:RabbitMQ:HostName"] = "rabbit.example.test",
      ["Messaging:RabbitMQ:UserName"] = "service-user",
      ["Messaging:RabbitMQ:Password"] = "service-password",
      ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5"
    };
    settings.Remove($"Messaging:RabbitMQ:{missingSetting}");

    using var host = CreateHost(settings);
    var act = () => host.StartAsync();

    await act.Should().ThrowAsync<OptionsValidationException>()
        .WithMessage("*BootStrapServers*");
  }

  [Theory]
  [InlineData("http://user:password@localhost:5672/")]
  [InlineData("amqp://user@localhost:5672/")]
  [InlineData("amqp://:password@localhost:5672/")]
  public async Task AddRabbitMQMessaging_ShouldRejectInvalidBootstrapUriAtHostStartup(string bootstrapUri)
  {
    using var host = CreateHost(new Dictionary<string, string?>
    {
      ["Messaging:RabbitMQ:BootStrapServers"] = bootstrapUri,
      ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5"
    });
    var act = () => host.StartAsync();

    await act.Should().ThrowAsync<OptionsValidationException>();
  }

  [Theory]
  [InlineData("0")]
  [InlineData("-1")]
  public async Task AddRabbitMQMessaging_ShouldRejectNonPositivePublisherConfirmTimeoutAtHostStartup(string timeout)
  {
    using var host = CreateHost(new Dictionary<string, string?>
    {
      ["Messaging:RabbitMQ:BootStrapServers"] = "amqp://service:secret@localhost:5672/",
      ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = timeout
    });
    var act = () => host.StartAsync();

    await act.Should().ThrowAsync<OptionsValidationException>()
        .WithMessage("*PublisherConfirmTimeoutSeconds*");
  }

  [Fact]
  public async Task AddRabbitMQMessaging_ShouldRejectNonPositiveHeartbeatAtHostStartup()
  {
    using var host = CreateHost(new Dictionary<string, string?>
    {
      ["Messaging:RabbitMQ:BootStrapServers"] = "amqp://service:secret@localhost:5672/",
      ["Messaging:RabbitMQ:RequestedHeartbeatSeconds"] = "0",
      ["Messaging:RabbitMQ:PublisherConfirmTimeoutSeconds"] = "5"
    });
    var act = () => host.StartAsync();

    await act.Should().ThrowAsync<OptionsValidationException>()
        .WithMessage("*RequestedHeartbeatSeconds*");
  }

  [Fact]
  public void ConnectionFactoryProvider_ShouldEnableStrictTlsForAmqpsBootstrapUri()
  {
    var options = Options.Create(new RabbitMQMessagingOptions
    {
      BootStrapServers = "amqps://service:secret@rabbit.example.test:5671/",
      PublisherConfirmTimeoutSeconds = 5
    });
    var provider = new ConnectionFactoryProvider(options, new Franz.Common.Reflection.AssemblyAccessorWrapper());

    var factory = (global::RabbitMQ.Client.ConnectionFactory)provider.Current;

    factory.Ssl.Enabled.Should().BeTrue();
    factory.Ssl.AcceptablePolicyErrors.Should().Be(System.Net.Security.SslPolicyErrors.None);
  }

  private static IHost CreateHost(Dictionary<string, string?> settings)
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(settings)
        .Build();

    return new HostBuilder()
        .ConfigureServices((_, services) => services.AddRabbitMQMessaging(configuration))
        .Build();
  }

  [Fact]
  public void RabbitMQMessagingOptions_ShouldDefaultToSequentialConsumption()
  {
    new RabbitMQMessagingOptions().MaxConcurrency.Should().Be(1);
  }

  [Theory]
  [InlineData("0")]
  [InlineData("-1")]
  [InlineData("65536")]
  public async Task AddRabbitMQMessaging_ShouldRejectOutOfRangeConcurrencyAtHostStartup(string concurrency)
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Messaging:RabbitMQ:BootStrapServers"] = _fixture.ConnectionString,
          ["Messaging:RabbitMQ:MaxConcurrency"] = concurrency
        })
        .Build();

    using var host = new HostBuilder()
        .ConfigureServices((_, services) => services.AddRabbitMQMessaging(configuration))
        .Build();

    var act = () => host.StartAsync();

    await act.Should().ThrowAsync<OptionsValidationException>()
        .WithMessage("*MaxConcurrency*");
  }

  [Fact]
  public void AddRabbitMQMessaging_ShouldBeIdempotent()
  {
    _services.AddRabbitMQMessaging(_configuration);
    _services.AddRabbitMQMessaging(_configuration); // called twice

    // Check idempotent registrations
    _services.Count(s => s.ServiceType == typeof(IMessagingPublisher)).Should().Be(1);
    _services.Count(s => s.ServiceType == typeof(IChannelPool)).Should().Be(1);
    _services.Count(s => s.ServiceType == typeof(IConnectionProvider)).Should().Be(1);
    _services.Count(s => s.ServiceType == typeof(IModelProvider)).Should().Be(1);
  }
}