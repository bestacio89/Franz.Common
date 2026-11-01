using FluentAssertions;
using Franz.Common.Business.Helpers;
using Franz.Common.Mediator.Dispatchers;
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
        typeof(string).Assembly);

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
  public void AddFranzMediatorDefault_RegistersOnlyGeneratedClosedGenericPipelines()
  {
    var services = new ServiceCollection();

    services.AddFranzMediatorDefault<PingCommandHandler>();

    services.Should().NotContain(descriptor =>
        descriptor.ServiceType == typeof(Franz.Common.Mediator.Pipelines.Core.IPipeline<,>) ||
        descriptor.ServiceType == typeof(Franz.Common.Mediator.Pipelines.Core.INotificationPipeline<>) ||
        descriptor.ServiceType == typeof(Franz.Common.Mediator.Pipelines.Processors.IPreProcessor<>) ||
        descriptor.ServiceType == typeof(Franz.Common.Mediator.Pipelines.Processors.IPostProcessor<,>));

    services.Should().Contain(descriptor =>
        descriptor.ServiceType == typeof(Franz.Common.Mediator.Pipelines.Core.IPipeline<
            Franz.Common.Mediator.Messages.ICommand<string>, string>) &&
        descriptor.ImplementationType == typeof(Franz.Common.Mediator.Pipelines.Logging.LoggingPipeline<
            Franz.Common.Mediator.Messages.ICommand<string>, string>));
  }

  [Fact]
  public void AddFranzMediatorV1_AcceptsGeneratedAssemblyWithNoHandlers()
  {
    var services = new ServiceCollection();

    var act = () => services.AddFranzMediator(
        new[] { typeof(GeneratedHandlerRegistrationProvider).Assembly });

    act.Should().NotThrow();
    services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(IDispatcher));
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
  [Fact]
  public async Task GeneratedDispatchers_KeepCommandAndQueryResponseContractsSeparate()
  {
    var services = new ServiceCollection();
    services.AddFranzMediator(new[] { typeof(DualRequestHandler).Assembly });
    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

    var commandResult = await dispatcher.SendAsync<string>(new DualRequest(21));
    var queryResult = await dispatcher.SendAsync<int>(new DualRequest(21));

    commandResult.Should().Be("command:21");
    queryResult.Should().Be(42);
  }

  [Fact]
  public void LegacyHandlerCollector_UsesGeneratedTransientRegistrations()
  {
    var services = new ServiceCollection();

    HandlerCollector.CollectHandlers(services, typeof(PingCommandHandler).Assembly);

    services.Should().ContainSingle(descriptor =>
        descriptor.ServiceType == typeof(ICommandHandler<PingCommand, string>)
        && descriptor.ImplementationType == typeof(PingCommandHandler)
        && descriptor.Lifetime == ServiceLifetime.Transient);
  }

  public sealed record DualRequest(int Value) : Franz.Common.Mediator.Messages.ICommand<string>, Franz.Common.Mediator.Messages.IQuery<int>;

  public sealed class DualRequestHandler : ICommandHandler<DualRequest, string>, IQueryHandler<DualRequest, int>
  {
    public Task<string> Handle(DualRequest request, CancellationToken cancellationToken)
        => Task.FromResult($"command:{request.Value}");

    Task<int> IQueryHandler<DualRequest, int>.Handle(DualRequest query, CancellationToken cancellationToken)
        => Task.FromResult(query.Value * 2);
  }

}
