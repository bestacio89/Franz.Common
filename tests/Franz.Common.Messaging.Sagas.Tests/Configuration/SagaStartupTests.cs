using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Messaging.Sagas.Tests.Configuration;

public sealed class SagaStartupTests
{
  [Fact]
  public async Task BuildFranzSagas_ValidatesMappingsWithoutResolvingScopedServicesFromRoot()
  {
    var middlewareInvoked = false;
    var services = new ServiceCollection();

    var sagaBuilder = services.AddFranzSagas();
    sagaBuilder.AddSaga<StartupSaga>();
    sagaBuilder.UsePipeline(async next =>
    {
      middlewareInvoked = true;
      await next();
    });

    services.AddScoped<IMessagingPublisher, TestMessagingPublisher>();

    using var provider = services.BuildServiceProvider(validateScopes: true);

    var hostedService = provider.GetServices<IHostedService>().Single();
    await hostedService.StartAsync(CancellationToken.None);
    provider.BuildFranzSagas();

    var pipeline = provider.GetRequiredService<SagaExecutionPipeline>();
    await pipeline.ExecuteAsync(() => Task.CompletedTask);

    middlewareInvoked.Should().BeTrue();
    provider.GetRequiredService<SagaRouter>().Should().NotBeNull();
  }


  [Fact]
  public void AddSaga_RegistersOnlyClosedNotificationHandlersForDeclaredIntegrationEvents()
  {
    var services = new ServiceCollection();

    services.AddFranzSagas().AddSaga<StartupSaga>();

    var notificationHandlerType = typeof(INotificationHandler<StartEvent>);
    var descriptor = services
        .Where(service => service.ServiceType == notificationHandlerType)
        .Should()
        .ContainSingle()
        .Subject;

    descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    descriptor.ImplementationType.Should().NotBeNull();
    descriptor.ImplementationType!.IsGenericType.Should().BeTrue();
    descriptor.ImplementationType.GetGenericArguments().Should().ContainSingle()
        .Which.Should().Be(typeof(StartEvent));

    services.Should().NotContain(service =>
        service.ServiceType == typeof(INotificationHandler<>));
  }

  private sealed class StartupSaga : SagaBase<StartupSagaState>, IStartWith<StartEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        StartEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Continue());
  }

  public sealed class StartupSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record StartEvent : IIntegrationEvent;

  private sealed class TestMessagingPublisher : IMessagingPublisher
  {
    public ValueTask Publish<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken ct = default)
        where TIntegrationEvent : IIntegrationEvent
        => ValueTask.CompletedTask;
  }
}
