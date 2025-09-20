using Franz.Common.Caching.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Estrategies;
public class NamespacedCacheKeyStrategy : ICacheKeyStrategy
{
  private readonly string _namespace;
  public NamespacedCacheKeyStrategy(string ns) => _namespace = ns;
  public string GetKey(object request) => $"{_namespace}:{request.GetType().Name}:{request.GetHashCode()}";
  public string BuildKey<TRequest>(TRequest request)
  {
    var type = typeof(TRequest).Name;
    var payload = JsonSerializer.Serialize(request);

    return $"{_namespace}:{type}:{payload}";
  }
}