using FluentAssertions;
using Franz.Common.Mediator;
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

    services.AddFranzSagas(builder =>
    {
      builder.AddSaga<StartupSaga>();
      builder.UsePipeline(async next =>
      {
        middlewareInvoked = true;
        await next();
      });
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
