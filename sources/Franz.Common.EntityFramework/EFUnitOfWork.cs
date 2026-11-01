using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Franz.Common.EntityFramework;

/// <summary>
/// Coordinates EF Core changes and an optional database transaction for one unit of work.
/// </summary>
public class EfUnitOfWork<TDbContext> : IUnitOfWork, IAsyncDisposable
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public EfUnitOfWork(TDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Persists pending changes and commits a transaction owned by this unit of work.
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var transaction = _transaction;
        int affectedRows;

        try
        {
            affectedRows = await _db.SaveChangesAsync(ct);

            if (transaction is not null)
                await transaction.CommitAsync(ct);
        }
        catch (Exception operationException)
        {
            if (transaction is not null)
            {
                Exception? cleanupException = null;

                try
                {
                    // Cleanup must still run when the request token was cancelled.
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    cleanupException = exception;
                }

                try
                {
                    await ReleaseTransactionAsync(transaction);
                }
                catch (Exception exception)
                {
                    cleanupException = cleanupException is null
                        ? exception
                        : new AggregateException(cleanupException, exception);
                }

                if (cleanupException is not null)
                {
                    throw new AggregateException(
                        "The unit-of-work operation failed and transaction cleanup also failed.",
                        operationException,
                        cleanupException);
                }
            }

            throw;
        }

        if (transaction is not null)
            await ReleaseTransactionAsync(transaction);

        return affectedRows;
    }

    /// <inheritdoc />
    public async Task BeginAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already active for this unit of work.");

        if (_db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException(
                "The DbContext already has an active transaction that is not owned by this unit of work.");

        _transaction = await _db.Database.BeginTransactionAsync(ct);
    }

    async Task IUnitOfWork.CommitAsync(CancellationToken ct)
        => _ = await CommitAsync(ct);

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var transaction = _transaction;
        if (transaction is null)
            return;

        try
        {
            var rollbackToken = ct.IsCancellationRequested
                ? CancellationToken.None
                : ct;

            await transaction.RollbackAsync(rollbackToken);
        }
        finally
        {
            await ReleaseTransactionAsync(transaction);
        }
    }

    /// <summary>
    /// Rolls back any transaction left active when the owning scope is disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        var transaction = _transaction;
        if (transaction is null)
            return;

        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        finally
        {
            await ReleaseTransactionAsync(transaction);
        }
    }

    private async ValueTask ReleaseTransactionAsync(IDbContextTransaction transaction)
    {
        if (ReferenceEquals(_transaction, transaction))
            _transaction = null;

        await transaction.DisposeAsync();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
