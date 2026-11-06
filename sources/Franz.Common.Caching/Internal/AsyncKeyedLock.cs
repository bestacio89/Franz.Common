namespace Franz.Common.Caching.Internal;

/// <summary>
/// A process-local asynchronous keyed lock. Entries are reference-counted under
/// a single short-lived gate so idle keys are removed without racing new waiters.
/// </summary>
internal sealed class AsyncKeyedLock<TKey> where TKey : notnull
{
  private readonly object _gate = new();
  private readonly Dictionary<TKey, Entry> _entries = new();

  public async ValueTask<Releaser> AcquireAsync(TKey key, CancellationToken cancellationToken)
  {
    Entry entry;

    lock (_gate)
    {
      if (!_entries.TryGetValue(key, out entry!))
      {
        entry = new Entry();
        _entries.Add(key, entry);
      }

      entry.ReferenceCount++;
    }

    try
    {
      await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
      return new Releaser(this, key, entry);
    }
    catch
    {
      ReleaseReference(key, entry);
      throw;
    }
  }

  private void Release(TKey key, Entry entry)
  {
    entry.Semaphore.Release();
    ReleaseReference(key, entry);
  }

  private void ReleaseReference(TKey key, Entry entry)
  {
    lock (_gate)
    {
      entry.ReferenceCount--;

      if (entry.ReferenceCount == 0)
      {
        _entries.Remove(key);
        entry.Semaphore.Dispose();
      }
    }
  }

  internal sealed class Entry
  {
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public int ReferenceCount { get; set; }
  }

  internal sealed class Releaser : IDisposable
  {
    private AsyncKeyedLock<TKey>? _owner;
    private readonly TKey _key;
    private readonly Entry _entry;

    internal Releaser(AsyncKeyedLock<TKey> owner, TKey key, Entry entry)
    {
      _owner = owner;
      _key = key;
      _entry = entry;
    }

    public void Dispose()
    {
      Interlocked.Exchange(ref _owner, null)?.Release(_key, _entry);
    }
  }
}
