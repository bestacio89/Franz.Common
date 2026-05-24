using Franz.Common.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework;

public class EfUnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContext
{
  private readonly TDbContext _db;

  public EfUnitOfWork(TDbContext db)
  {
    _db = db;
  }

  public Task<int> CommitAsync(CancellationToken ct = default)
      => _db.SaveChangesAsync(ct);
}