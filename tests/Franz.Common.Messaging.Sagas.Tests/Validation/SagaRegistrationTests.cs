using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;

namespace Franz.Common.Messaging.Sagas.Tests.Validation;

public sealed class SagaRegistrationTests
{
  [Fact]
  public void FromType_ResolvesExplicitInterfaceStartHandler()
  {
    var registration = SagaRegistration.FromType(typeof(ExplicitStartSaga));

    registration.StartHandlers.Should().ContainKey(typeof(StartEvent));
    registration.StartHandlers[typeof(StartEvent)].IsPrivate.Should().BeTrue();
  }

  private sealed class ExplicitStartSaga : SagaBase<ExplicitStartSagaState>, IStartWith<StartEvent>
  {
    Task<ISagaTransition> IStartWith<StartEvent>.HandleAsync(
      StartEvent message,
      ISagaContext context,
      CancellationToken cancellationToken)
      => Task.FromResult<ISagaTransition>(context.Continue());
  }

  private sealed class ExplicitStartSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record StartEvent : IIntegrationEvent;
}
