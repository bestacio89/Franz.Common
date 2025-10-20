namespace Franz.Common.Business.Domain;
public interface IReadRepository<TEntity>
  where TEntity : IEntity
{
   Task<IReadOnlyList<TEntity>> GetAll(CancellationToken cancellation);
   Task <TEntity> GetEntity(int id);
}
