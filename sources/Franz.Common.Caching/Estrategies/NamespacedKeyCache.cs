using Franz.Common.Caching.Abstractions;

namespace Franz.Common.Caching.Estrategies;

public class NamespacedCacheKeyStrategy : ICacheKeyStrategy
{
  private readonly string _namespace;
  private readonly ICacheKeyStrategy _inner;

  public NamespacedCacheKeyStrategy(string ns)
      : this(ns, new DefaultCacheKeyStrategy())
  {
  }

  public NamespacedCacheKeyStrategy(string ns, ICacheKeyStrategy inner)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(ns);
    _namespace = ns;
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
  }

  public string GetKey(object request)
  {
    ArgumentNullException.ThrowIfNull(request);
    return Prefix(_inner.GetKey(request));
  }

  public string BuildKey<TRequest>(TRequest request)
  {
    ArgumentNullException.ThrowIfNull(request);
    return Prefix(_inner.BuildKey(request));
  }

  private string Prefix(string key) => $"{_namespace}:{key}";
}
