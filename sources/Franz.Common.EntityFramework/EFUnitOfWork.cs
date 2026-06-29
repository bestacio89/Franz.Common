using Franz.Common.EntityFramework;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Franz.Common.EntityFramework;

public class EfUnitOfWork<TDbContext> :
    IUnitOfWork
    where TDbContext : DbContext
{
  private readonly TDbContext _db;
  private IDbContextTransaction? _transaction;

  public EfUnitOfWork(TDbContext db)
  {
    _db = db;
  }

  // Franz.Common.EntityFramework.IUnitOfWork
  public Task<int> CommitAsync(CancellationToken ct = default)
      => _db.SaveChangesAsync(ct);

  // Franz.Common.Mediator.Pipelines.Core.IUnitOfWork
  public async Task BeginAsync(CancellationToken ct = default)
      => _transaction = await _db.Database.BeginTransactionAsync(ct);

  async Task IUnitOfWork.CommitAsync(CancellationToken ct)
  {
    await _db.SaveChangesAsync(ct);
    if (_transaction is not null)
    {
      await _transaction.CommitAsync(ct);
      await _transaction.DisposeAsync();
      _transaction = null;
    }
  }

  public async Task RollbackAsync(CancellationToken ct = default)
  {
    if (_transaction is not null)
    {
      await _transaction.RollbackAsync(ct);
      await _transaction.DisposeAsync();
      _transaction = null;
    }
  }
}