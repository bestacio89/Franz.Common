using FluentAssertions;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;
using Franz.Common.Messaging.Sagas.Validation;

namespace Franz.Common.Messaging.Sagas.Tests.Validation;

public sealed class SagaTypeValidatorTests
{
  [Fact]
  public void ValidateSagaType_AcceptsSagaBaseWithGuidIdentity()
  {
    var act = () => SagaTypeValidator.ValidateSagaType(typeof(ValidSaga));

    act.Should().NotThrow();
  }

  private sealed class ValidSaga : SagaBase<ValidSagaState>
  {
  }

  private sealed class ValidSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }
}
