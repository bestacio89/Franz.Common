using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Persistence.Memory;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Messaging.Sagas.Tests.Fixtures;

public sealed class SagaRuntimeFixture
{
  public SagaOrchestrator Orchestrator { get; }
  public InMemorySagaStateStore StateStore { get; }

  public SagaRuntimeFixture()
  {
    // REAL saga persistence
    StateStore = new InMemorySagaStateStore();

    var serializer = new JsonSagaStateSerializer();
    var repository = new InMemorySagaRepository(StateStore, serializer);

    // REAL execution pipeline
    var pipeline = new SagaExecutionPipeline();

    // REAL service provider
    var services = new ServiceCollection();
    services.AddSingleton<TestSaga>();
    var serviceProvider = services.BuildServiceProvider();

    // REAL router + registration
    var router = new SagaRouter(serviceProvider);
    router.RegisterSaga(typeof(TestSaga));

    // REAL orchestrator
    Orchestrator = new SagaOrchestrator(
      router,
      repository,
      pipeline,
      publisher: null!, // allowed: only used when outgoing is IIntegrationEvent
      serviceProvider);
  }
}
