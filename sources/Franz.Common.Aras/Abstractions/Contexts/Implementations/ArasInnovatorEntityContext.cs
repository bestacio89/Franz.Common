using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Mappings.Contracts.Factories;
using Franz.Common.Aras.Mappings.Factories;
using Franz.Common.Business.Domain;
using System.Net.Http.Json;

namespace Franz.Common.Aras.Innovator.Contexts;

public class ArasInnovatorEntityContext : IArasEntityContext, IDisposable
{
  private readonly HttpClient _client;
  private readonly IArasEntityMapperFactory _mapperFactory;

  public ArasInnovatorEntityContext(HttpClient client, IArasEntityMapperFactory mapperFactory)
  {
    _client = client;
    _mapperFactory = mapperFactory;
  }

  public async Task<IReadOnlyCollection<TEntity>> QueryEntitiesAsync<TEntity>(
      string query,
      CancellationToken ct = default
  ) where TEntity : Entity<Guid>
  {
    var response = await _client.GetAsync($"/api/v1/{typeof(TEntity).Name}?query={query}", ct);
    response.EnsureSuccessStatusCode();

    var arasPayload = await response.Content
        .ReadFromJsonAsync<List<Dictionary<string, object>>>(cancellationToken: ct)
        ?? new List<Dictionary<string, object>>();

    var mapper = _mapperFactory.Resolve<TEntity>();
    return arasPayload.Select(mapper.MapFromAras).ToList();
  }

  public async Task<TEntity?> GetEntityByIdAsync<TEntity>(
      Guid id,
      CancellationToken ct = default
  ) where TEntity : Entity<Guid>
  {
    var response = await _client.GetAsync($"/api/v1/{typeof(TEntity).Name}/{id}", ct);
    if (!response.IsSuccessStatusCode) return null;

    var arasData = await response.Content
        .ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ct);

    if (arasData is null) return null;

    var mapper = _mapperFactory.Resolve<TEntity>();
    return mapper.MapFromAras(arasData);
  }

  public async Task SaveEntityAsync<TEntity>(
      TEntity entity,
      CancellationToken ct = default
  ) where TEntity : Entity<Guid>
  {
    var mapper = _mapperFactory.Resolve<TEntity>();
    var arasData = mapper.MapToAras(entity);

    HttpResponseMessage response;
    if (entity.Id == Guid.Empty)
      response = await _client.PostAsJsonAsync($"/api/v1/{typeof(TEntity).Name}", arasData, ct);
    else
      response = await _client.PutAsJsonAsync($"/api/v1/{typeof(TEntity).Name}/{entity.Id}", arasData, ct);

    response.EnsureSuccessStatusCode();
  }

  public async Task DeleteEntityAsync<TEntity>(
      Guid id,
      CancellationToken ct = default
  ) where TEntity : Entity<Guid>
  {
    var response = await _client.DeleteAsync($"/api/v1/{typeof(TEntity).Name}/{id}", ct);
    response.EnsureSuccessStatusCode();
  }

  public void Dispose() => _client.Dispose();
}
