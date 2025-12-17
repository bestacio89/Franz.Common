using Franz.Common.Business.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.EntityTests;

internal sealed class TestEntity : Entity<Guid>
{
  public TestEntity(Guid id)
  {
    Id = id;
  }

  public TestEntity() { }
}

