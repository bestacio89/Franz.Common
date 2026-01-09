using Franz.Common.AzureCosmosDB.Conventions;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.AzureCosmosDB.Context;

public abstract class CosmosDbContextBase : DbContextBase
{
  protected CosmosDbContextBase(
      DbContextOptions options,
      IDispatcher dispatcher,
      ICurrentUserService? currentUser = null
  ) : base(options, dispatcher, currentUser)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // 1️⃣ Apply fallback container only (safe for all)
    ConfigureFallbackContainer(modelBuilder);

    // 2️⃣ Apply Cosmos-specific conventions (container, PK, JSON)
    modelBuilder.ApplyCosmosConventions();
  }

  /// <summary>
  /// Sets a safe default container for entities that do not declare their own.
  /// This prevents EF from throwing when no container is configured.
  /// </summary>
  private static void ConfigureFallbackContainer(ModelBuilder modelBuilder)
  {
    // Default fallback container, apply once globally.
    modelBuilder.HasDefaultContainer("franz");
  }
}
