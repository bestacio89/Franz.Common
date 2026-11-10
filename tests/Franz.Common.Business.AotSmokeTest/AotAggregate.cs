using Franz.Common.Business.Domain;

namespace Franz.Common.Business.AotSmokeTest;

public class AotAggregate : AggregateRoot<AotEvent>
{
  protected AotAggregate(Guid id) : base(id) { }
}