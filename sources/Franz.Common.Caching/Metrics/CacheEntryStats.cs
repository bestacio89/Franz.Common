using System.Collections.Concurrent;

namespace Franz.Common.Caching.Metrics;

public sealed class CacheEntryStats
{
  private readonly object _tagsGate = new();
  private int _sets;
  private int _hits;
  private long _lastSetBinary = long.MinValue;
  private long _lastAccessBinary = long.MinValue;
  private long _estimatedSizeBytes;

  public int Sets { get => Volatile.Read(ref _sets); set => Interlocked.Exchange(ref _sets, value); }
  public int Hits { get => Volatile.Read(ref _hits); set => Interlocked.Exchange(ref _hits, value); }

  public DateTime? LastSet
  {
    get { var value = Interlocked.Read(ref _lastSetBinary); return value == long.MinValue ? null : DateTime.FromBinary(value); }
    set => Interlocked.Exchange(ref _lastSetBinary, value?.ToBinary() ?? long.MinValue);
  }

  public DateTime? LastAccess
  {
    get { var value = Interlocked.Read(ref _lastAccessBinary); return value == long.MinValue ? null : DateTime.FromBinary(value); }
    set => Interlocked.Exchange(ref _lastAccessBinary, value?.ToBinary() ?? long.MinValue);
  }

  public long EstimatedSizeBytes
  {
    get => Interlocked.Read(ref _estimatedSizeBytes);
    set => Interlocked.Exchange(ref _estimatedSizeBytes, value);
  }

  public ConcurrentBag<string> Tags { get; } = new();

  public double HitRate
  {
    get
    {
      var hits = Hits;
      var sets = Sets;
      return hits + sets == 0 ? 0 : (double)hits / (hits + sets);
    }
  }

  public void RecordSet(long estimatedSizeBytes, IEnumerable<string>? tags = null)
  {
    Interlocked.Increment(ref _sets);
    EstimatedSizeBytes = estimatedSizeBytes;
    LastSet = DateTime.UtcNow;
    lock (_tagsGate)
    {
      Tags.Clear();
      if (tags is not null)
        foreach (var tag in tags) Tags.Add(tag);
    }
  }

  public void RecordHit()
  {
    Interlocked.Increment(ref _hits);
    LastAccess = DateTime.UtcNow;
  }

  public void AddTags(IEnumerable<string>? tags)
  {
    if (tags is null) return;
    lock (_tagsGate)
      foreach (var tag in tags) Tags.Add(tag);
  }

  public void Reset()
  {
    Interlocked.Exchange(ref _sets, 0);
    Interlocked.Exchange(ref _hits, 0);
    LastSet = null;
    LastAccess = null;
    EstimatedSizeBytes = 0;
    lock (_tagsGate) Tags.Clear();
  }

  public CacheEntryStats Snapshot()
  {
    var snapshot = new CacheEntryStats
    {
      Sets = Sets,
      Hits = Hits,
      LastSet = LastSet,
      LastAccess = LastAccess,
      EstimatedSizeBytes = EstimatedSizeBytes
    };
    snapshot.AddTags(Tags);
    return snapshot;
  }
}
