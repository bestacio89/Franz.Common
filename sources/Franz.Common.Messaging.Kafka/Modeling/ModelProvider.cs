#nullable enable
using Franz.Common.Messaging.Kafka.Connections;
using System.Threading;

namespace Franz.Common.Messaging.Kafka.Modeling;

/// <summary>
/// Provides a thread-safe, lazily-initialized KafkaModel.
/// Implements IAsyncDisposable with a hardened idempotency pattern to prevent ObjectDisposedException.
/// </summary>
public sealed class ModelProvider : IModelProvider, IAsyncDisposable
{
  private readonly IConnectionFactoryProvider _connectionFactoryProvider;
  private readonly SemaphoreSlim _lock = new(1, 1);
  private KafkaModel? _model;
  private bool _disposed;

  public ModelProvider(IConnectionFactoryProvider connectionFactoryProvider)
  {
    _connectionFactoryProvider = connectionFactoryProvider ?? throw new ArgumentNullException(nameof(connectionFactoryProvider));
  }

  public KafkaModel Current => GetCurrent();

  private KafkaModel GetCurrent()
  {
    // 1. Check for disposal first to fail fast
    if (_disposed) throw new ObjectDisposedException(nameof(ModelProvider));


    if (_model != null) return _model;

    _lock.Wait();
    try
    {
      // 3. Double-check after acquiring lock
      if (_model == null)
      {
        _model = new KafkaModel(_connectionFactoryProvider);
      }
    }
    finally
    {
      _lock.Release();
    }

    return _model;
  }

  public async ValueTask DisposeAsync()
  {

    if (_disposed) return;

    await _lock.WaitAsync();
    try
    {
      
      if (_disposed) return;

      if (_model != null)
      {
        // Ensure the underlying Kafka producer is flushed and disposed
        await _model.DisposeAsync();
        _model = null;
      }

      _disposed = true;
    }
    finally { 

      _lock.Release();
    }

    GC.SuppressFinalize(this);
  }
}