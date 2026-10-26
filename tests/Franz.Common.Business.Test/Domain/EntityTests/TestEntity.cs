using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.EntityTests;

internal class TestEntity : Entity<Guid>
{
  protected TestEntity(Guid id) : base(id) { }


  public TestEntity() { }
}

