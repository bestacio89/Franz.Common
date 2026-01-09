#nullable enable

using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

public sealed class SagaRuntimeFixture
{
  public SagaOrchestrator Orchestrator { get; }
  public InMemorySagaStateStore StateStore { get; }
  public JsonSagaStateSerializer Serializer { get; }

  public SagaRuntimeFixture(InMemorySagaStateStore? sharedStore = null)
  {
    // =========================
    // Persistence
    // =========================
    StateStore = sharedStore ?? new InMemorySagaStateStore();
    Serializer = new JsonSagaStateSerializer();
    var repository = new InMemorySagaRepository(StateStore, Serializer);

    // =========================
    // IoC container
    // =========================
    var services = new ServiceCollection();

    // Real mediator
    services.AddFranzMediator(new[]
    {
      typeof(StartEvent).Assembly
    });

    // Saga type
    services.AddSingleton<TestSaga>();

    // Build provider
    var serviceProvider = services.BuildServiceProvider();

    // =========================
    // Router
    // =========================
    var router = new SagaRouter(serviceProvider);
    router.RegisterSagasFromAssembly(typeof(TestSaga).Assembly);
    ;

    // =========================
    // Pipeline
    // =========================
    var pipeline = new SagaExecutionPipeline();

    // =========================
    // Orchestrator
    // =========================
    Orchestrator = new SagaOrchestrator(
        router,
        repository,
        pipeline,
        publisher: NullMessagingPublisher.Instance,
        serviceProvider);
  }
}
