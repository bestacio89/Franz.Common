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

  [Fact]
  public async Task Saga_executes_end_to_end_inside_real_rabbitmq_host()
  {
    var publisher = _fixture.Services.GetRequiredService<IMessagingPublisher>();
    var stateStore = _fixture.StateStore;
    var serializer = _fixture.Serializer;

    var id = "saga-rabbit-1";

    // ========================
    // Publish real events
    // ========================
    await publisher.Publish(new StartEvent(id));
    await publisher.Publish(new StepEvent(id));

    // ========================
    // Wait for saga side effects
    // ========================
    TestSagaState? state = null;
    var timeout = TimeSpan.FromSeconds(20);
    var start = DateTime.UtcNow;

    while (DateTime.UtcNow - start < timeout)
    {
      if (stateStore.Store.TryGetValue(id, out var json) && json != null)
      {
        state = (TestSagaState?)serializer.Deserialize(json, typeof(TestSagaState));
        break;
      }
      await Task.Delay(200);
    }

    // ========================
    // Assert
    // ========================
    Assert.NotNull(state);
    Assert.Equal(id, state!.Id);
    Assert.Equal(2, state.Counter);   // StartEvent + StepEvent
  }
}
