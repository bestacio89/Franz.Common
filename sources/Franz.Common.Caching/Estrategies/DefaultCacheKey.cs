using Franz.Common.Caching.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Estrategies;
public class DefaultCacheKeyStrategy : ICacheKeyStrategy
{
  public string GetKey(object request) =>
      $"mediator:{request.GetType().Name}:{request.GetHashCode()}";
  public string BuildKey<TRequest>(TRequest request)
  {
    var type = typeof(TRequest).FullName ?? typeof(TRequest).Name;

    // Serialize request parameters to make the key unique
    var payload = JsonSerializer.Serialize(request);

    return $"{type}:{payload}";
  }
}