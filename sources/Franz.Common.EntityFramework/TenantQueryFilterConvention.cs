using Franz.Common.Business.Domain;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Linq.Expressions;

namespace Franz.Common.EntityFramework;

/// <summary>
/// Composes Franz persistence filters with the application's existing unnamed
/// query filter after the derived DbContext has completed model configuration.
/// A single unnamed filter is retained for EF Core 10 compatibility.
/// </summary>
internal sealed class TenantQueryFilterConvention(DbContextBase dbContext) : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isDomainEntity = ImplementsIEntity(clrType);
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(clrType);
            var applyTenantIsolation = isTenantEntity && dbContext.MultiTenancyEnabledForModel;

            if (!isDomainEntity && !applyTenantIsolation)
                continue;

            var applicationFilter = entityType.GetQueryFilter();
            var parameter = applicationFilter?.Parameters[0]
                ?? Expression.Parameter(clrType, "entity");
            var filterBody = applicationFilter?.Body ?? Expression.Constant(true);

            if (isDomainEntity)
            {
                var isDeleted = Expression.Property(parameter, nameof(IEntity<object>.IsDeleted));
                var softDeletionFilter = Expression.Equal(isDeleted, Expression.Constant(false));
                filterBody = Expression.AndAlso(filterBody, softDeletionFilter);
            }

            if (applyTenantIsolation)
            {
                var tenantId = Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    new[] { typeof(Guid) },
                    parameter,
                    Expression.Constant(nameof(ITenantEntity.TenantId)));
                var currentTenantId = Expression.Property(
                    Expression.Constant(dbContext),
                    nameof(DbContextBase.CurrentTenantId));

                filterBody = Expression.AndAlso(
                    filterBody,
                    Expression.Equal(tenantId, currentTenantId));
            }

            entityType.SetQueryFilter(Expression.Lambda(filterBody, parameter));
        }
    }

    private static bool ImplementsIEntity(Type type)
        => type.GetInterfaces().Any(
            contract => contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IEntity<>));
}
