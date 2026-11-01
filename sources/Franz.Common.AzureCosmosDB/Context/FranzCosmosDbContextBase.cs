using Franz.Common.AzureCosmosDB.Conventions;
using Franz.Common.AzureCosmosDB.Options;
using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Franz.Common.AzureCosmosDB.Context;

/// <summary>
/// Cosmos EF context with tenant isolation inherited from DbContextBase.
/// Tenant-owned documents use Cosmos-native tenant partitioning.
/// </summary>
public abstract class CosmosDbContextBase : DbContextBase
{
    private readonly CosmosOptions _cosmosOptions;

    /// <summary>
    /// Retains the original constructor signature for existing derived contexts.
    /// </summary>
    protected CosmosDbContextBase(
        DbContextOptions options,
        IDispatcher dispatcher,
        IOptions<CosmosOptions> cosmosOptions,
        ICurrentUserService? currentUser = null)
        : this(options, dispatcher, cosmosOptions, currentUser, null)
    {
    }

    /// <summary>
    /// Creates a Cosmos context with an optional validated tenant execution-context accessor.
    /// </summary>
    protected CosmosDbContextBase(
        DbContextOptions options,
        IDispatcher dispatcher,
        IOptions<CosmosOptions> cosmosOptions,
        ICurrentUserService? currentUser,
        ITenantExecutionContextAccessor? tenantContextAccessor)
        : base(options, dispatcher, currentUser, tenantContextAccessor)
    {
        _cosmosOptions = cosmosOptions?.Value
            ?? throw new ArgumentNullException(nameof(cosmosOptions));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureFallbackContainer(modelBuilder);
        modelBuilder.ApplyCosmosConventions();
    }

    private void ConfigureFallbackContainer(ModelBuilder modelBuilder)
    {
        var defaultContainer = !string.IsNullOrWhiteSpace(_cosmosOptions.DatabaseName)
            ? _cosmosOptions.DatabaseName.ToLowerInvariant()
            : "franz";

        modelBuilder.HasDefaultContainer(defaultContainer);
    }
}
