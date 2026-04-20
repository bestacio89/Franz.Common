using Franz.Common.Business.Domain;

namespace Franz.Common.EntityFramework.Tests.Repositories.Fakes;

public class DummyAggregate : AggregateRoot<DummyEvent>
{
  public string Name { get; private set; } =
      string.Empty;

  public DummyAggregate()
  {
    Register<DummyEvent>(Apply);
  }

  public DummyAggregate(Guid id)
      : base(id)
  {
    Register<DummyEvent>(Apply);
  }

  private void Apply(DummyEvent ev)
  {
    Name =
        $"UpdatedByEvent-{ev.AggregateId}";
  }

  public void DoSomething()
  {
    RaiseEvent(
        new DummyEvent
        {
          AggregateId = Id
        });
  }
}