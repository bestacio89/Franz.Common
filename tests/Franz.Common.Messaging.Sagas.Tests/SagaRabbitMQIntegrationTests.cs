#nullable enable

using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Sagas.Configuration;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Fixtures;
using Franz.Common.Messaging.Sagas.Tests.Sagas;

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Messaging.Sagas.Tests.Integration;

public sealed class SagaRabbitMqIntegrationTests : IClassFixture<SagaRabbitMQMongoFixture>
{
  private readonly SagaRabbitMQMongoFixture _fixture;

  public SagaRabbitMqIntegrationTests(SagaRabbitMQMongoFixture fixture)
  {
    _fixture = fixture;
  }

  private async Task<TestSagaState?> WaitForStateAsync(string id)
  {
    var repository = _fixture.Services.GetRequiredService<ISagaRepository>();
    var start = DateTime.UtcNow;

    while (DateTime.UtcNow - start < TimeSpan.FromSeconds(10))
    {
      var loaded = await repository.LoadStateAsync(id, typeof(TestSagaState), CancellationToken.None);

      if (loaded is TestSagaState s)
        return s;

      await Task.Delay(200);
    }

    return null;
  }

  // ----------------------------------------------------------
  // 1) Saga starts and persists initial state
  // ----------------------------------------------------------
  [Fact]
  public async Task StartEvent_creates_and_persists_saga_state()
  {
    // Host already started by fixture
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    var id = "saga-start-1";
    await dispatcher.PublishNotificationAsync(new StartEvent(id));

    var state = await WaitForStateAsync(id);

    Assert.NotNull(state);
    Assert.Equal(1, state!.Counter);
  }

  // ----------------------------------------------------------
  // 2) Saga full lifecycle (start + step)
  // ----------------------------------------------------------
  [Fact]
  public async Task Saga_executes_full_lifecycle_inside_real_rabbitmq_host()
  {
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    var id = "saga-lifecycle-1";
    await dispatcher.PublishNotificationAsync(new StartEvent(id));
    await dispatcher.PublishNotificationAsync(new StepEvent(id));

    var state = await WaitForStateAsync(id);

    Assert.NotNull(state);
    Assert.Equal(2, state!.Counter);
  }

  // ----------------------------------------------------------
  // 3) CompensationEvent correctly reverts saga state
  // ----------------------------------------------------------
  [Fact]
  public async Task CompensationEvent_reverts_state()
  {
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    var id = "saga-comp-1";
    await dispatcher.PublishNotificationAsync(new StartEvent(id));
    await dispatcher.PublishNotificationAsync(new StepEvent(id));
    await dispatcher.PublishNotificationAsync(new CompensationEvent(id));

    var state = await WaitForStateAsync(id);

    Assert.NotNull(state);
    Assert.Equal(1, state!.Counter); // reverted from 2 → 1
  }

  // ----------------------------------------------------------
  // 4) Saga state survives orchestrator reconstruction
  // ----------------------------------------------------------
  [Fact]
  public async Task Saga_state_survives_orchestrator_recreation()
  {
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    var id = "saga-survival-1";
    await dispatcher.PublishNotificationAsync(new StartEvent(id));

    var state1 = await WaitForStateAsync(id);
    Assert.NotNull(state1);
    Assert.Equal(1, state1!.Counter);

    // Simulate orchestrator restart
    _fixture.Services.BuildFranzSagas(); // 🔥 rebuild routing table

    // Continue saga
    await dispatcher.PublishNotificationAsync(new StepEvent(id));

    var state2 = await WaitForStateAsync(id);

    Assert.NotNull(state2);
    Assert.Equal(2, state2!.Counter); // continues from stored value
  }
}
