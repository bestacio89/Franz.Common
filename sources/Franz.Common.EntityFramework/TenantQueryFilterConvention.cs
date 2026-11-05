using Franz.Common.Business.Domain;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Linq.Expressions;

namespace Franz.Common.EntityFramework;

/// <summary>
/// Composes Franz persistence predicates with application query filters after
/// the derived DbContext has completed model configuration. The convention
/// preserves the model's filter style so EF Core 10 never mixes named and
/// unnamed filters on the same entity type.
/// </summary>
internal sealed class TenantQueryFilterConvention(DbContextBase dbContext) : IModelFinalizingConvention
{
    private const string FrameworkQueryFilterName = "Franz.Common.Persistence";

    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            // EF Core query filters are defined on the root of an inheritance hierarchy.
            if (entityType.BaseType is not null)
                continue;

            var clrType = entityType.ClrType;
            var isDomainEntity = ImplementsIEntity(clrType);
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(clrType);
            var applyTenantIsolation = isTenantEntity && dbContext.MultiTenancyEnabledForModel;

            if (!isDomainEntity && !applyTenantIsolation)
                continue;

            var applicationFilters = entityType.GetDeclaredQueryFilters();
            var namedApplicationFilters = applicationFilters
                .Where(filter => !filter.IsAnonymous)
                .ToArray();
            var anonymousApplicationFilter = applicationFilters
                .SingleOrDefault(filter => filter.IsAnonymous);

            if (namedApplicationFilters.Length > 0 && anonymousApplicationFilter is not null)
            {
                throw new InvalidOperationException(
                    $"Entity '{clrType.FullName}' has both named and unnamed application query filters. " +
                    "EF Core 10 does not support mixing these filter styles on the same entity type.");
            }

            var parameter = anonymousApplicationFilter?.Expression.Parameters[0]
                ?? Expression.Parameter(clrType, "entity");
            var filterBody = anonymousApplicationFilter?.Expression.Body ?? Expression.Constant(true);

            if (isDomainEntity)
            {
                var isDeleted = Expression.Property(parameter, nameof(IEntity<object>.IsDeleted));
                filterBody = Expression.AndAlso(
                    filterBody,
                    Expression.Equal(isDeleted, Expression.Constant(false)));
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

            var composedFilter = Expression.Lambda(filterBody, parameter);

            if (namedApplicationFilters.Length == 0)
            {
                entityType.SetQueryFilter(composedFilter);
                continue;
            }

            if (namedApplicationFilters.Any(filter => filter.Key == FrameworkQueryFilterName))
            {
                throw new InvalidOperationException(
                    $"Entity '{clrType.FullName}' already declares the reserved query filter name " +
                    $"'{FrameworkQueryFilterName}'. Rename the application filter to avoid a collision.");
            }

            entityType.SetQueryFilter(FrameworkQueryFilterName, composedFilter);
        }
    }

    private static bool ImplementsIEntity(Type type)
        => type.GetInterfaces().Any(
            contract => contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IEntity<>));
}
