#nullable enable

using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.Cosmos;

/// <summary>
/// Coordinates one EF Cosmos SaveChanges batch. All saga, inbox, and outbox documents
/// are mapped to the same container and tenant partition so Cosmos can commit them atomically.
/// </summary>
public sealed class CosmosSagaUnitOfWork(CosmosSagaDbContext dbContext) : IUnitOfWork
{
  private readonly CosmosSagaDbContext _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  private bool _active;

  internal bool IsActive => _active;

  public Task BeginAsync(CancellationToken cancellationToken = default)
  {
    if (_active)
      throw new InvalidOperationException("A Cosmos saga persistence unit is already active.");
    cancellationToken.ThrowIfCancellationRequested();
    _active = true;
    return Task.CompletedTask;
  }

  public async Task CommitAsync(CancellationToken cancellationToken = default)
  {
    if (!_active)
      throw new InvalidOperationException("No Cosmos saga persistence unit is active.");

    try
    {
      await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      _active = false;
    }
    catch
    {
      _db.ChangeTracker.Clear();
      _active = false;
      throw;
    }
  }

  public Task RollbackAsync(CancellationToken cancellationToken = default)
  {
    _db.ChangeTracker.Clear();
    _active = false;
    return Task.CompletedTask;
  }
}
