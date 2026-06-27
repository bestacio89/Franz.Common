using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.Factories;

public interface IEntityFactory<TKey, TEntity>
    where TEntity : Entity<TKey>
{
  TEntity Create();
}