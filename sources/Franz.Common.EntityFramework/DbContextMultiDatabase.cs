using Franz.Common.EntityFramework;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;

[Obsolete("Use DbContextBase instead of DbContextMultiDatabase")]
public class DbContextMultiDatabase : DbContext
{
  private readonly IDispatcher dispatcher;

  public DbContextMultiDatabase(DbContextOptions dbContextOptions, IDispatcher dispatcher)
      : base(dbContextOptions)
  {
    this.dispatcher = dispatcher;
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

    return await base.SaveChangesAsync(cancellationToken);
  }

  public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
  {
    await SaveChangesAsync(cancellationToken);

    // 🔹 Use the extension method here
    await dispatcher.DispatchDomainEventsAsync(this, cancellationToken);

    return true;
  }
}
