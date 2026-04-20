using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.Factories;

public interface IEntityFactory<TId, TEntity>
    where TEntity : Entity<TId>
{
  TEntity Create();
}