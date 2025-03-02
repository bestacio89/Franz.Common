// ✅ General CRUD repository for individual entities
using Franz.Common.Business.Domain;

public interface IEntityRepository<TEntity> where TEntity : class, IEntity
{
}