using Franz.Common.Business.Domain.IdGenerators;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Domain.Factories;

public sealed class EntityFactory<TId, TEntity> : IEntityFactory<TId, TEntity>
    where TEntity : Entity<TId>, new()
{
  private readonly IIdGenerator<TId> _idGenerator;

  public EntityFactory(IIdGenerator<TId> idGenerator)
  {
    _idGenerator = idGenerator;
  }

  public TEntity Create()
  {
    var entity = new TEntity();

    entity.SetId(_idGenerator.Create());

    return entity;
  }
}