// ✅ General CRUD repository for individual entities
using Franz.Common.Business.Domain;
namespace Franz.Common.Business.Repositories;
public interface IEntityRepository<TEntity> where TEntity : class, IEntity
{
}