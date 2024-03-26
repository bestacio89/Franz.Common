using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework;
[Obsolete("Use DbContextBase instead of DbContextMultiDatabase")]
public class DbContextMultiDatabase : DbContext
{
  private readonly IMediator mediator = default!;

  public DbContextMultiDatabase(DbContextOptions dbContextOptions, IMediator mediator)
    : base(dbContextOptions)
  {
    this.mediator = mediator;
  }

  protected DbContextMultiDatabase()
    : base()
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
  }

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    if (Database.CurrentTransaction is null)
      await Database.BeginTransactionAsync();

    var result = await base.SaveChangesAsync(cancellationToken);

    return result;
  }

  public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
  {
    await SaveChangesAsync(cancellationToken);

    await mediator.DispatchDomainEventsAsync(this, cancellationToken);

    return true;
  }
}
