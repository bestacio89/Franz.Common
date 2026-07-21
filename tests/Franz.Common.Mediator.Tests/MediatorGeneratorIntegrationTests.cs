using FluentAssertions;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace Franz.Common.Mediator.Tests.Generator;

public class MediatorGeneratorIntegrationTests
{
  [Fact]
  public void AddFranzGeneratedHandlerRegistration_ResolvesSourceGeneratedHandlersFromContainer()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act - Executes compile-time generated registrations emitted by Roslyn for this assembly
    services.AddFranzGeneratedHandlerRegistration();

    var provider = services.BuildServiceProvider();

    // Assert 1: Verify service descriptor was registered by source generator
    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICommandHandler<PingCommand, string>));
    descriptor.Should().NotBeNull();
    descriptor!.ImplementationType.Should().Be(typeof(PingCommandHandler));
    descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

    // Assert 2: Resolve instance directly from DI scope
    using var scope = provider.CreateScope();
    var handler = scope.ServiceProvider.GetService<ICommandHandler<PingCommand, string>>();

    handler.Should().NotBeNull();
    handler.Should().BeOfType<PingCommandHandler>();
  }
}