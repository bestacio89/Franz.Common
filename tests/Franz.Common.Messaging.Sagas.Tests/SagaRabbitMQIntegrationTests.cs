#nullable enable

using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Fixtures;
using Franz.Common.Messaging.Sagas.Persistence;
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

    var repo = _fixture.Services.GetRequiredService<ISagaRepository>();
    var start = DateTime.UtcNow;

    while (DateTime.UtcNow - start < timeout)
    {
      var state = await repo.LoadStateAsync(sagaId, typeof(TestSagaState), CancellationToken.None);
      if (state is TestSagaState typed)
        return typed;

      await Task.Delay(200);
    }

    return null;
  }

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

  [Fact]
  public async Task CompensationEvent_reverts_state()
  {
    var publisher = _fixture.Services.GetRequiredService<IMessagingPublisher>();
    var id = "saga-rmq-2";

    await publisher.Publish(new StartEvent(id));          // 1
    await publisher.Publish(new CompensationEvent(id));   // 0

    var state = await WaitForState(id);

    Assert.NotNull(state);
    Assert.Equal(0, state!.Counter);
  }

  [Fact]
  public async Task Saga_state_survives_orchestrator_recreation()
  {
    var publisher = _fixture.Services.GetRequiredService<IMessagingPublisher>();
    var id = "saga-rmq-3";

    await publisher.Publish(new StartEvent(id));
    Assert.Equal(1, (await WaitForState(id))!.Counter);

    var recreated = new SagaRabbitMQFixture();
    await recreated.InitializeAsync();

    var recreatedPublisher = recreated.Services.GetRequiredService<IMessagingPublisher>();
    await recreatedPublisher.Publish(new CompensationEvent(id));

    var finalState = await WaitForState(id);
    Assert.NotNull(finalState);
    Assert.Equal(0, finalState!.Counter);
  }

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
