using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Business.Domain;

namespace Franz.Common.Aras.Testing
{
  public class InMemoryArasEntityContext : IArasEntityContext
  {
    private readonly Dictionary<Type, Dictionary<Guid, object>> _store = new();

    public Task<IReadOnlyCollection<TEntity>> QueryEntitiesAsync<TEntity>(
        string query, CancellationToken ct = default)
        where TEntity : Entity<Guid>
    {
      if (_store.TryGetValue(typeof(TEntity), out var set))
      {
        var results = set.Values.Cast<TEntity>().ToList();
        return Task.FromResult((IReadOnlyCollection<TEntity>)results);
      }

      return Task.FromResult<IReadOnlyCollection<TEntity>>(Array.Empty<TEntity>());
    }

    public Task<TEntity?> GetEntityByIdAsync<TEntity>(
        Guid id, CancellationToken ct = default)
        where TEntity : Entity<Guid>
    {
      if (_store.TryGetValue(typeof(TEntity), out var set) &&
          set.TryGetValue(id, out var entity))
        return Task.FromResult((TEntity?)entity);

      return Task.FromResult<TEntity?>(null);
    }

    public Task SaveEntityAsync<TEntity>(TEntity entity, CancellationToken ct = default)
        where TEntity : Entity<Guid>
    {
      if (!_store.ContainsKey(typeof(TEntity)))
        _store[typeof(TEntity)] = new Dictionary<Guid, object>();

      _store[typeof(TEntity)][entity.Id] = entity;
      return Task.CompletedTask;
    }

    public Task DeleteEntityAsync<TEntity>(Guid id, CancellationToken ct = default)
        where TEntity : Entity<Guid>
    {
      if (_store.TryGetValue(typeof(TEntity), out var set))
        set.Remove(id);
      return Task.CompletedTask;
    }

    public void Dispose() { }
  }
}
