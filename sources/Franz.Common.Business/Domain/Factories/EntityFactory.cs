using Franz.Common.Business.Domain.IdGenerators;

namespace Franz.Common.Business.Domain.Factories;

public sealed class EntityFactory<TKey, TEntity> : IEntityFactory<TKey, TEntity>
    where TEntity : Entity<TKey>, new()
{
  private readonly IIdGenerator<TKey> _idGenerator;

  public EntityFactory(IIdGenerator<TKey> idGenerator)
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