#nullable enable

using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Fixtures;
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

  [Fact]
  public async Task Saga_executes_end_to_end_inside_real_rabbitmq_host()
  {
    // -------------------------------------------------
    // Arrange – start full host (RabbitMQ + Outbox + Sagas)
    // -------------------------------------------------
    await _fixture.StartAsync();

    var stateStore = _fixture.StateStore;          // InMemorySagaStateStore
    var stateSerializer = _fixture.SagaStateSerializer; // JsonSagaStateSerializer

    // Dispatcher is the entry point into the messaging pipeline:
    // StartEvent/StepEvent -> mediator -> messaging -> outbox -> RabbitMQ -> saga
    var dispatcher = _fixture.Services.GetRequiredService<IDispatcher>();

    var id = "saga-1";

    // -------------------------------------------------
    // Act – publish saga events through mediator
    // (serialization to Message is done by the messaging pipeline)
    // -------------------------------------------------
    await dispatcher.PublishNotificationAsync(new StartEvent(id));
    await dispatcher.PublishNotificationAsync(new StepEvent(id));

    // -------------------------------------------------
    // Wait for saga state to be materialized by the orchestrator
    // -------------------------------------------------
    TestSagaState? state = null;
    var start = DateTime.UtcNow;
    var timeout = TimeSpan.FromSeconds(10);

    while (DateTime.UtcNow - start < timeout)
    {
      if (stateStore.Store.TryGetValue(id, out var json) && json is not null)
      {
        // Here we deserialize *saga state*, so we use the saga state serializer
        state = (TestSagaState?)stateSerializer.Deserialize(json, typeof(TestSagaState));
        break;
      }

      await Task.Delay(200);
    }

    // -------------------------------------------------
    // Assert – the saga ran end-to-end correctly
    // -------------------------------------------------
    Assert.NotNull(state);
    Assert.Equal(id, state!.Id);
    Assert.Equal(2, state.Counter); // StartEvent + StepEvent
  }
}
