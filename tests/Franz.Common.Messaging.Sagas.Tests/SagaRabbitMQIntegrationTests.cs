#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Fixtures;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Messaging.Sagas.Tests.Integration;

public sealed class SagaRabbitMqIntegrationTests : IClassFixture<SagaRabbitMQFixture>
{
  private readonly SagaRabbitMQFixture _fixture;

  public SagaRabbitMqIntegrationTests(SagaRabbitMQFixture fixture)
  {
    _fixture = fixture;
  }

  private async Task<TestSagaState?> WaitForState(string sagaId, TimeSpan? timeout = null)
  {
    timeout ??= TimeSpan.FromSeconds(20);

    var stateStore = _fixture.StateStore;
    var serializer = _fixture.Serializer;

    var start = DateTime.UtcNow;

    while (DateTime.UtcNow - start < timeout)
    {
      if (stateStore.Store.TryGetValue(sagaId, out var json) && json != null)
      {
        return (TestSagaState?)serializer.Deserialize(json, typeof(TestSagaState));
      }

      await Task.Delay(200);
    }

    return null;
  }

  // ---------------------------------------------------------------------
  // TEST 1 — StartEvent creates saga state
  // ---------------------------------------------------------------------
  [Fact]
  public async Task StartEvent_creates_and_persists_saga_state()
  {
    var publisher = _fixture.Services.GetRequiredService<IMessagingPublisher>();
    var id = "saga-rmq-1";

    await publisher.Publish(new StartEvent(id));

    var state = await WaitForState(id);

    Assert.NotNull(state);
    Assert.Equal(id, state!.Id);
    Assert.Equal(1, state.Counter);
  }

  // ---------------------------------------------------------------------
  // TEST 2 — Compensation event modifies existing saga state
  // ---------------------------------------------------------------------
  [Fact]
  public async Task CompensationEvent_reverts_state()
  {
    var publisher = _fixture.Services.GetRequiredService<IMessagingPublisher>();
    var id = "saga-rmq-2";

    await publisher.Publish(new StartEvent(id));          // counter = 1
    await publisher.Publish(new CompensationEvent(id));   // counter = 0

    var state = await WaitForState(id);

    Assert.NotNull(state);
    Assert.Equal(0, state!.Counter);
  }

  // ---------------------------------------------------------------------
  // TEST 3 — Saga state survives orchestrator recreation
  // ---------------------------------------------------------------------
  [Fact]
  public async Task Saga_state_survives_orchestrator_recreation()
  {
    var publisher = _fixture.Services.GetRequiredService<IMessagingPublisher>();
    var id = "saga-rmq-3";

    await publisher.Publish(new StartEvent(id)); // counter = 1

    var before = await WaitForState(id);
    Assert.NotNull(before);
    Assert.Equal(1, before!.Counter);

    // ---- recreate fixture (broker still running, persistence store shared) ----
    var recreated = new SagaRabbitMQFixture();
    await recreated.InitializeAsync();
    var recreatedPublisher = recreated.Services.GetRequiredService<IMessagingPublisher>();

    await recreatedPublisher.Publish(new CompensationEvent(id)); // counter = 0

    var state = await WaitForState(id);

    Assert.NotNull(state);
    Assert.Equal(0, state!.Counter);
  }

  // ---------------------------------------------------------------------
  // TEST 4 — Full end-to-end lifecycle: Start + Compensation
  // ---------------------------------------------------------------------
  [Fact]
  public async Task Saga_executes_full_lifecycle_inside_real_rabbitmq_host()
  {
    var publisher = _fixture.Services.GetRequiredService<IMessagingPublisher>();
    var id = "saga-rmq-4";

    await publisher.Publish(new StartEvent(id));
    await publisher.Publish(new CompensationEvent(id));

    var state = await WaitForState(id);

    Assert.NotNull(state);
    Assert.Equal(id, state!.Id);
    Assert.Equal(0, state.Counter);
  }
}
