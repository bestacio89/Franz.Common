using FluentAssertions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Registration;
using Franz.Common.Mediator.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace Franz.Common.Mediator.Tests.Generator;

public class MediatorGeneratorIntegrationTests
{
  [Fact]
  public void AddFranzGeneratedHandlerRegistration_ResolvesSourceGeneratedHandlersFromContainer()
  {
    var services = new ServiceCollection();

    services.AddFranzGeneratedHandlerRegistration();

    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICommandHandler<PingCommand, string>));
    descriptor.Should().NotBeNull();
    descriptor!.ImplementationType.Should().Be(typeof(PingCommandHandler));
    descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var handler = scope.ServiceProvider.GetService<ICommandHandler<PingCommand, string>>();

    handler.Should().NotBeNull();
    handler.Should().BeOfType<PingCommandHandler>();
  }

  [Fact]
  public void GeneratedRegistration_DoesNotDiscoverHandlersFromUnrelatedAssemblies()
  {
    var services = new ServiceCollection();
    var provider = new GeneratedHandlerRegistrationProvider(
        typeof(GeneratedHandlerRegistrationProvider).Assembly);

    var act = () => provider.Register(services);

    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*no generated Franz Mediator registration contribution*");
    services.Should().BeEmpty();
  }

  [Fact]
  public void AddFranzMediatorV1_AutomaticallyRegistersGeneratedHandlersFromSelectedAssemblies()
  {
    var services = new ServiceCollection();

    services.AddFranzMediator(new[] { typeof(PingCommandHandler).Assembly });

    services.Should().ContainSingle(descriptor =>
        descriptor.ServiceType == typeof(ICommandHandler<PingCommand, string>)
        && descriptor.ImplementationType == typeof(PingCommandHandler)
        && descriptor.Lifetime == ServiceLifetime.Scoped);
  }

  [Fact]
  public void GeneratedRegistration_IsIdempotentAtServiceRegistrationLevel()
  {
    var services = new ServiceCollection();

    services.AddFranzGeneratedHandlerRegistration();
    services.AddFranzGeneratedHandlerRegistration();

    services.Should().ContainSingle(descriptor =>
        descriptor.ServiceType == typeof(ICommandHandler<PingCommand, string>)
        && descriptor.ImplementationType == typeof(PingCommandHandler));
  }
}
