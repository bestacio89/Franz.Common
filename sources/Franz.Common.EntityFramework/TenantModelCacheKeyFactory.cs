using Microsoft.EntityFrameworkCore;
using Franz.Common.EntityFramework.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Franz.Common.EntityFramework;

/// <summary>
/// Includes model-shaping tenancy settings in EF Core's model cache identity.
/// EF caches models by context type by default, which is insufficient when the
/// same context type can be configured for different tenancy policies.
/// </summary>
internal sealed class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public TenantModelCacheKeyFactory() { }

    public object Create(DbContext context, bool designTime)
        => context is DbContextBase dbContext
            ? (context.GetType(), dbContext.MultiTenancyEnabledForModel, dbContext.DatabaseIsolationModeForModel, designTime)
            : (context.GetType(), designTime);
}
