#nullable enable

using Franz.Common.Mediator.Pipelines.Core;
using MongoDB.Driver;

namespace Franz.Common.Messaging.Sagas.Persistence.Mongo;

/// <summary>
/// Owns one MongoDB multi-document transaction for saga state, inbox and outbox writes.
/// The deployment must support transactions (normally a replica set or sharded cluster).
/// </summary>
public sealed class MongoSagaUnitOfWork : IUnitOfWork, IAsyncDisposable
{
  private readonly IMongoClient _client;
  private IClientSessionHandle? _session;
  private bool _disposed;

  public MongoSagaUnitOfWork(IMongoClient client)
      => _client = client ?? throw new ArgumentNullException(nameof(client));

  internal IClientSessionHandle? CurrentSession => _session;

  public async Task BeginAsync(CancellationToken cancellationToken = default)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    if (_session is not null)
      throw new InvalidOperationException("A MongoDB saga transaction is already active.");

    _session = await _client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    _session.StartTransaction(new TransactionOptions(
        readConcern: ReadConcern.Snapshot,
        readPreference: ReadPreference.Primary,
        writeConcern: WriteConcern.WMajority));
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    var session = _session
        ?? throw new InvalidOperationException("No MongoDB saga transaction is active.");

    try
    {
      await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
    }
    catch
    {
      try
      {
        await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
      }
      catch
      {
        // Preserve the commit failure; disposal still releases the session.
      }

      throw;
    }
    finally
    {
      await ReleaseSessionAsync().ConfigureAwait(false);
    }
  }

  public async Task RollbackAsync(CancellationToken cancellationToken = default)
  {
    var session = _session;
    if (session is null)
      return;

    try
    {
      await session.AbortTransactionAsync(
          cancellationToken.IsCancellationRequested ? CancellationToken.None : cancellationToken)
          .ConfigureAwait(false);
    }
    finally
    {
      await ReleaseSessionAsync().ConfigureAwait(false);
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed)
      return;

    _disposed = true;
    if (_session is not null)
    {
      try
      {
        await _session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
      }
      finally
      {
        await ReleaseSessionAsync().ConfigureAwait(false);
      }
    }
  }

  private ValueTask ReleaseSessionAsync()
  {
    var session = _session;
    _session = null;
    session?.Dispose();
    return ValueTask.CompletedTask;
  }
}
