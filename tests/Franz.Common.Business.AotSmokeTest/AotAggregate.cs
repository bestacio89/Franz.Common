using Franz.Common.Business.Domain;

namespace Franz.Common.Business.AotSmokeTest;

public sealed class AotAggregate : AggregateRoot<AotEvent>
{
  protected AotAggregate(Guid id) : base(id) { }
}