using Franz.Common.AzureCosmosDB.Conventions;
using Franz.Common.AzureCosmosDB.Options; // New Reference
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options; // New Reference

namespace Franz.Common.AzureCosmosDB.Context;

public abstract class CosmosDbContextBase : DbContextBase
{
  private readonly CosmosOptions _cosmosOptions;

  protected CosmosDbContextBase(
      DbContextOptions options,
      IDispatcher dispatcher,
      IOptions<CosmosOptions> cosmosOptions, // Injecting the validated options
      ICurrentUserService? currentUser = null
  ) : base(options, dispatcher, currentUser)
  {
    _cosmosOptions = cosmosOptions.Value;
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);


    // 1️⃣ Apply the dynamic fallback container from configuration
    ConfigureFallbackContainer(modelBuilder);

    // 2️⃣ Apply Cosmos-specific conventions (container, PK, JSON)
    modelBuilder.ApplyCosmosConventions();
  }

  /// <summary>
  /// Sets a dynamic default container from CosmosOptions.
  /// This ensures the "Franz" ecosystem follows the user's config.
  /// </summary>
  private void ConfigureFallbackContainer(ModelBuilder modelBuilder)
  {
    // If the user hasn't specified a specific messaging container, 
    // we use a safe default or a configured root container name.
    var defaultContainer = !string.IsNullOrWhiteSpace(_cosmosOptions.DatabaseName)
        ? _cosmosOptions.DatabaseName.ToLowerInvariant()
        : "franz";

    modelBuilder.HasDefaultContainer(defaultContainer);
  }
}