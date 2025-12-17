using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.AggregateRootTests;

internal sealed class TestAggregate : AggregateRoot<TestEvent>
{
  public string? Name { get; private set; }

  public TestAggregate()
  {
    Register<TestEvent>(Apply);
  }

  public void Create(string name)
  {
    RaiseEvent(new TestEvent(name));
  }

  private void Apply(TestEvent e)
  {
    Name = e.Name;
  }
}

