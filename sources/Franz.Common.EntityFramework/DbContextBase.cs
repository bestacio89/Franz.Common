using Franz.Common.EntityFramework.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.EntityFramework;
public class DbContextBase : DbContext
{
  private readonly IMediator mediator = default!;

  public DbContextBase(DbContextOptions dbContextOptions, IMediator mediator)
    : base(dbContextOptions)
  {
    this.mediator = mediator;
  }

  protected DbContextBase()
    : base()
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder
      .ApplyConfigurationsFromAssembly(GetType().Assembly)
      .ConvertEnumeration();
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
