using FluentAssertions;
using Franz.Common.Headers;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Messaging.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
  private readonly IServiceCollection _services = new ServiceCollection();

  [Fact]
  public void AddMessagingHeaderContext_ShouldRegisterHeaderContextAccessor()
  {
    _services.AddMessagingHeaderContext();

    _services.Should().ContainSingle(descriptor =>
        descriptor.ServiceType == typeof(IHeaderContextAccessor) &&
        descriptor.ImplementationType == typeof(HeaderContextAccessor) &&
        descriptor.Lifetime == ServiceLifetime.Scoped);
  }

  [Fact]
  public void AddMessagingSerialization_ShouldRegisterScopedJsonSerializerFactory()
  {
    _services.AddMessagingSerialization();

    _services.Should().ContainSingle(descriptor =>
        descriptor.ServiceType == typeof(IMessageSerializer) &&
        descriptor.ImplementationFactory != null &&
        descriptor.Lifetime == ServiceLifetime.Scoped);

    using var provider = _services.BuildServiceProvider(validateScopes: true);
    using var scope = provider.CreateScope();
    scope.ServiceProvider.GetRequiredService<IMessageSerializer>()
        .Should().BeOfType<JsonMessageSerializer>();
  }

  [Fact]
  public void AddMessagingFactories_ShouldRegisterScopedFactoryAndAllStrategies()
  {
    _services.AddMessagingFactories();

    _services.Should().ContainSingle(descriptor =>
        descriptor.ServiceType == typeof(IMessageFactory) &&
        descriptor.ImplementationFactory != null &&
        descriptor.Lifetime == ServiceLifetime.Scoped);

    var strategies = _services.Where(descriptor =>
        descriptor.ServiceType == typeof(IMessageBuilderStrategy)).ToArray();
    strategies.Should().HaveCount(4);
    strategies.Should().OnlyContain(descriptor =>
        descriptor.ImplementationFactory != null &&
        descriptor.Lifetime == ServiceLifetime.Scoped);

    using var provider = _services.BuildServiceProvider(
        new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    using var scope = provider.CreateScope();
    scope.ServiceProvider.GetRequiredService<IMessageFactory>()
        .Should().BeOfType<MessageFactory>();
    scope.ServiceProvider.GetServices<IMessageBuilderStrategy>()
        .Should().HaveCount(4);
  }
}
