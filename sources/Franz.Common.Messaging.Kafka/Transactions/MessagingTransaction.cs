#nullable enable
using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka.Transactions;

/// <summary>
/// Owns one isolated Kafka producer for the lifetime of a transaction.
/// An unfinished transaction is aborted during disposal.
/// </summary>
public sealed class MessagingTransaction : IMessagingTransaction
{
  private readonly IKafkaTransactionProducerFactory? _producerFactory;
  private readonly KafkaTransactionContext? _context;
  private IProducer<string, byte[]>? _producer;
  private readonly IProducer<string, byte[]>? _legacyProducer;
  private bool _isActive;
  private bool _disposed;
  private readonly SemaphoreSlim _gate = new(1, 1);

  /// <summary>Compatibility constructor for direct callers; DI uses the isolated-producer constructor.</summary>
  public MessagingTransaction(IProducer<string, byte[]> producer)
  {
    _legacyProducer = producer ?? throw new ArgumentNullException(nameof(producer));
  }

  public MessagingTransaction(
      IKafkaTransactionProducerFactory producerFactory,
      KafkaTransactionContext context)
  {
    _producerFactory = producerFactory ?? throw new ArgumentNullException(nameof(producerFactory));
    _context = context ?? throw new ArgumentNullException(nameof(context));
  }

  public async Task BeginAsync(CancellationToken ct = default)
  {
    await _gate.WaitAsync(ct).ConfigureAwait(false);
    try
    {
      ThrowIfDisposed();
      if (_isActive)
        throw new InvalidOperationException("A Kafka transaction is already active.");

      var producer = _producerFactory?.Create() ?? _legacyProducer!;
      _producer = producer;
      try
      {
        _context?.SetProducer(producer);
        producer.BeginTransaction();
        _isActive = true;
      }
      catch
      {
        _context?.ClearProducer(producer);
        if (_producerFactory is not null)
          producer.Dispose();
        _producer = null;
        throw;
      }
    }
    finally
    {
      _gate.Release();
    }
  }

  public async Task CompleteAsync(CancellationToken ct = default)
  {
    await _gate.WaitAsync(ct).ConfigureAwait(false);
    try
    {
      if (!_isActive)
        return;

      var producer = _producer!;
      try
      {
        producer.CommitTransaction();
      }
      finally
      {
        _isActive = false;
        ReleaseProducer(producer);
      }
    }
    finally
    {
      _gate.Release();
    }
  }

  public async Task RollbackAsync(CancellationToken ct = default)
  {
    await _gate.WaitAsync(ct).ConfigureAwait(false);
    try
    {
      if (!_isActive)
        return;

      var producer = _producer!;
      try
      {
        producer.AbortTransaction();
      }
      finally
      {
        _isActive = false;
        ReleaseProducer(producer);
      }
    }
    finally
    {
      _gate.Release();
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed)
      return;

    if (_isActive)
      await RollbackAsync().ConfigureAwait(false);

    _disposed = true;
    _gate.Dispose();
  }

  private void ReleaseProducer(IProducer<string, byte[]> producer)
  {
    _context?.ClearProducer(producer);
    if (_producerFactory is not null)
      producer.Dispose();
    _producer = null;
  }

  private void ThrowIfDisposed()
  {
    if (_disposed)
      throw new ObjectDisposedException(nameof(MessagingTransaction));
  }
}
