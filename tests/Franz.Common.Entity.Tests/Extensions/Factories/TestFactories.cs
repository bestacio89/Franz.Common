using Franz.Common.Business.Domain;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.EntityFramework.Tests.Extensions.Factories;

public static class TestFactories
{
  public static EntityFactory<Guid, T> CreateEntityFactory<T>()
      where T : Entity<Guid>, new()
  {
    return new EntityFactory<Guid, T>(
        new GuidV7Generator());
  }
}