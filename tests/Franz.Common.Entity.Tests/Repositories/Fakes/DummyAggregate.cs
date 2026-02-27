using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests.Repositories.Fakes;


public class DummyAggregate : AggregateRoot<DummyEvent>
{
  public string Name { get; private set; } = string.Empty;

  public DummyAggregate()
  {
    // Register event handler
    Register<DummyEvent>(Apply);
  }

  private void Apply(DummyEvent ev)
  {
    // For test purposes, we can simulate some state change
    Name = $"UpdatedByEvent-{ev.AggregateId}";
  }

  // Helper method to raise a new event
  public void DoSomething()
  {
    var ev = new DummyEvent { AggregateId = PersistentId };
    RaiseEvent(ev);
  }
}