#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

public sealed class SagaRuntimeFixture
{
  public SagaOrchestrator Orchestrator { get; }
  public InMemorySagaStateStore StateStore { get; }

  public SagaRuntimeFixture(InMemorySagaStateStore? sharedStore = null)
  {
    // =========================
    // Saga persistence
    // =========================
    StateStore = sharedStore ?? new InMemorySagaStateStore();

    var serializer = new JsonSagaStateSerializer();
    var repository = new InMemorySagaRepository(StateStore, serializer);

    // =========================
    // Execution pipeline
    // =========================
    var pipeline = new SagaExecutionPipeline();

    // =========================
    // Service provider
    // =========================
    var services = new ServiceCollection();
    services.AddSingleton<TestSaga>();

    var serviceProvider = services.BuildServiceProvider();

    // =========================
    // Router + registration
    // =========================
    var router = new SagaRouter(serviceProvider);
    router.RegisterSaga(typeof(TestSaga));

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
