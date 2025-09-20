using Franz.Common.Caching.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Settings;
public class SettingsCache : ISettingsCache
{
  private readonly ICacheProvider _cache;
  public SettingsCache(ICacheProvider cache) => _cache = cache;

  public Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default) =>
      _cache.GetAsync<T>($"settings:{key}", ct);

  public Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default) =>
      _cache.SetAsync($"settings:{key}", value, TimeSpan.FromHours(12), ct);
}
