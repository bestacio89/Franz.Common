using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Business.Domain;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Franz.Common.Aras.Diagnostics
{
  public class DiagnosticEntityContextDecorator : IArasEntityContext
  {
    private readonly IArasEntityContext _inner;
    private readonly ILogger<DiagnosticEntityContextDecorator> _logger;
    private readonly ActivitySource _activitySource;

    public DiagnosticEntityContextDecorator(
        IArasEntityContext inner,
        ILogger<DiagnosticEntityContextDecorator> logger,
        ActivitySource activitySource)
    {
      _inner = inner;
      _logger = logger;
      _activitySource = activitySource;
    }

    public async Task<IReadOnlyCollection<TEntity>> QueryEntitiesAsync<TEntity>(
        string query,
        CancellationToken ct = default
    ) where TEntity : Entity<Guid>
    {
      using var activity = _activitySource.StartActivity("Aras.QueryEntities");
      activity?.SetTag("entity", typeof(TEntity).Name);
      activity?.SetTag("query", query);

      _logger.LogInformation("Executing ARAS query for {Entity}: {Query}", typeof(TEntity).Name, query);

      var result = await _inner.QueryEntitiesAsync<TEntity>(query, ct);

      _logger.LogInformation("Retrieved {Count} {Entity} entities", result.Count, typeof(TEntity).Name);
      return result;
    }

    public async Task<TEntity?> GetEntityByIdAsync<TEntity>(Guid id, CancellationToken ct = default)
        where TEntity : Entity<Guid>
    {
      using var activity = _activitySource.StartActivity("Aras.GetEntity");
      activity?.SetTag("entity", typeof(TEntity).Name);
      activity?.SetTag("id", id);

      _logger.LogInformation("Fetching ARAS {Entity} with Id {Id}", typeof(TEntity).Name, id);

      var result = await _inner.GetEntityByIdAsync<TEntity>(id, ct);

      _logger.LogInformation("Fetched {Entity} with Id {Id}: Found={Found}",
          typeof(TEntity).Name, id, result != null);

      return result;
    }

    public async Task SaveEntityAsync<TEntity>(TEntity entity, CancellationToken ct = default)
        where TEntity : Entity<Guid>
    {
      using var activity = _activitySource.StartActivity("Aras.SaveEntity");
      activity?.SetTag("entity", typeof(TEntity).Name);
      activity?.SetTag("id", entity.Id);

      _logger.LogInformation("Saving ARAS {Entity} with Id {Id}", typeof(TEntity).Name, entity.Id);

      await _inner.SaveEntityAsync(entity, ct);

      _logger.LogInformation("Saved ARAS {Entity} with Id {Id}", typeof(TEntity).Name, entity.Id);
    }

    public async Task DeleteEntityAsync<TEntity>(Guid id, CancellationToken ct = default)
        where TEntity : Entity<Guid>
    {
      using var activity = _activitySource.StartActivity("Aras.DeleteEntity");
      activity?.SetTag("entity", typeof(TEntity).Name);
      activity?.SetTag("id", id);

      _logger.LogWarning("Deleting ARAS {Entity} with Id {Id}", typeof(TEntity).Name, id);

      await _inner.DeleteEntityAsync<TEntity>(id, ct);

      _logger.LogInformation("Deleted ARAS {Entity} with Id {Id}", typeof(TEntity).Name, id);
    }

    public void Dispose() => _inner.Dispose();
  }
}
