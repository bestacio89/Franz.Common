#nullable enable

using Franz.Common.Messaging.Sagas.Tests.Events;
using Franz.Common.Messaging.Sagas.Tests.Sagas;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Franz.Common.Messaging.Sagas.Tests.Fixtures;
using Xunit;

namespace Franz.Common.Messaging.Sagas.Tests.Integration;

public sealed class SagaIntegrationTests
{
  private readonly SagaRuntimeFixture _fixture;
  private readonly JsonSagaStateSerializer _serializer = new();

  public SagaIntegrationTests()
  {
    _fixture = new SagaRuntimeFixture();
  }

  [Fact]
  public async Task StartEvent_creates_and_persists_saga_state()
  {
    // Act
    await _fixture.Orchestrator.HandleEventAsync(
      new StartEvent("saga-1"),
      correlationId: "saga-1",
      causationId: null,
      CancellationToken.None);

    // Assert
    var json = _fixture.StateStore.Store["saga-1"];
    var state = (TestSagaState)_serializer.Deserialize(json, typeof(TestSagaState));

    Assert.Equal("saga-1", state.Id);
    Assert.Equal(1, state.Counter);
  }

  [Fact]
  public async Task StepEvent_loads_state_and_advances_saga()
  {
    // Arrange
    await _fixture.Orchestrator.HandleEventAsync(
      new StartEvent("saga-2"),
      correlationId: "saga-2",
      causationId: null,
      CancellationToken.None);

    // Act
    await _fixture.Orchestrator.HandleEventAsync(
      new StepEvent("saga-2"),
      correlationId: "saga-2",
      causationId: null,
      CancellationToken.None);

    // Assert
    var json = _fixture.StateStore.Store["saga-2"];
    var state = (TestSagaState)_serializer.Deserialize(json, typeof(TestSagaState));

    Assert.Equal(2, state.Counter);
  }

  [Fact]
  public async Task CompensationEvent_reverts_state()
  {
    // Arrange
    await _fixture.Orchestrator.HandleEventAsync(
      new StartEvent("saga-3"),
      correlationId: "saga-3",
      causationId: null,
      CancellationToken.None);

    await _fixture.Orchestrator.HandleEventAsync(
      new StepEvent("saga-3"),
      correlationId: "saga-3",
      causationId: null,
      CancellationToken.None);

    // Act
    await _fixture.Orchestrator.HandleEventAsync(
      new CompensationEvent("saga-3"),
      correlationId: "saga-3",
      causationId: null,
      CancellationToken.None);

    // Assert
    var json = _fixture.StateStore.Store["saga-3"];
    var state = (TestSagaState)_serializer.Deserialize(json, typeof(TestSagaState));

    Assert.Equal(1, state.Counter);
  }

  [Fact]
  public async Task Saga_state_survives_orchestrator_recreation()
  {
    // Arrange
    await _fixture.Orchestrator.HandleEventAsync(
      new StartEvent("saga-4"),
      correlationId: "saga-4",
      causationId: null,
      CancellationToken.None);

    // New orchestrator, same store
    var newFixture = new SagaRuntimeFixture(_fixture.StateStore);

    // Act
    await newFixture.Orchestrator.HandleEventAsync(
      new StepEvent("saga-4"),
      correlationId: "saga-4",
      causationId: null,
      CancellationToken.None);

    // Assert
    var json = newFixture.StateStore.Store["saga-4"];
    var state = (TestSagaState)_serializer.Deserialize(json, typeof(TestSagaState));

    Assert.Equal(2, state.Counter);
  }
}
