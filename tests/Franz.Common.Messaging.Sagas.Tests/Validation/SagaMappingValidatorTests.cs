using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Validation;

namespace Franz.Common.Messaging.Sagas.Tests.Validation;

public sealed class SagaMappingValidatorTests
{
  [Fact]
  public void ValidateRegistration_RejectsMessageMappedAsBothStartAndStep()
  {
    var registration = SagaRegistration.FromType(typeof(AmbiguousStartAndStepSaga));

    var act = () => SagaMappingValidator.ValidateRegistration(registration);

    act.Should().Throw<Exception>()
        .WithMessage("*cannot be mapped as both a start and a step*");
  }

  [Fact]
  public void ValidateRegistration_RejectsMessageMappedAsBothStepAndCompensation()
  {
    var registration = SagaRegistration.FromType(typeof(AmbiguousStepAndCompensationSaga));

    var act = () => SagaMappingValidator.ValidateRegistration(registration);

    act.Should().Throw<Exception>()
        .WithMessage("*cannot be mapped as both a step and a compensation*");
  }

  public sealed class AmbiguousStartAndStepSaga :
      SagaBase<TestSagaState>,
      IStartWith<SharedEvent>,
      IHandle<SharedEvent>,
      ISagaIdResolver<SharedEvent>
  {
    public Guid ResolveSagaId(SharedEvent message) => message.SagaId;

    public Task<ISagaTransition> HandleAsync(
        SharedEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Continue());
  }

  public sealed class AmbiguousStepAndCompensationSaga :
      SagaBase<TestSagaState>,
      IHandle<SharedEvent>,
      ICompensateWith<SharedEvent>,
      ISagaIdResolver<SharedEvent>
  {
    public Guid ResolveSagaId(SharedEvent message) => message.SagaId;

    public Task<ISagaTransition> HandleAsync(
        SharedEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Continue());
  }

  public sealed class TestSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  public sealed record SharedEvent(Guid SagaId) : IIntegrationEvent;
}
