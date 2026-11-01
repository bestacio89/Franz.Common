using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Franz.Common.AzureCosmosDB.Conventions;

/// <summary>
/// Applies Cosmos-specific container, partition-key and JSON naming conventions.
/// Tenant-owned documents use TenantId as their first partition-key component.
/// </summary>
public static class CosmosModelBuilderExtensions
{
    public static ModelBuilder ApplyCosmosConventions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (clr is null || clr.Namespace?.StartsWith("Microsoft", StringComparison.Ordinal) == true)
                continue;

            var builder = modelBuilder.Entity(clr);
            builder.ToContainer(clr.Name.ToLowerInvariant());

            var isTenantOwned = typeof(ITenantEntity).IsAssignableFrom(clr);
            var hasCustomPartitionKey = typeof(ICosmosPartitionKey).IsAssignableFrom(clr);

            if (isTenantOwned)
            {
                var tenantIdProperty = clr.GetProperty(nameof(ITenantEntity.TenantId));
                if (tenantIdProperty is null || tenantIdProperty.PropertyType != typeof(Guid))
                {
                    throw new InvalidOperationException(
                        $"Tenant-owned Cosmos entity '{clr.Name}' must expose a public Guid TenantId property.");
                }

                // Cosmos partition keys support string, numeric and Boolean values.
                // Preserve the framework Guid contract and serialize it as a string.
                builder.Property<Guid>(nameof(ITenantEntity.TenantId)).HasConversion<string>();

                if (hasCustomPartitionKey)
                {
                    var partitionKeyProperty = clr.GetProperty("PartitionKey");
                    if (partitionKeyProperty is null || partitionKeyProperty.PropertyType != typeof(string))
                    {
                        throw new InvalidOperationException(
                            $"Cosmos entity '{clr.Name}' implementing ICosmosPartitionKey must expose a public string PartitionKey property.");
                    }

                    // Tenant first, then the application's finer-grained partition component.
                    builder.HasPartitionKey(nameof(ITenantEntity.TenantId), "PartitionKey");
                }
                else
                {
                    builder.HasPartitionKey(nameof(ITenantEntity.TenantId));
                }
            }
            else if (hasCustomPartitionKey)
            {
                var partitionKeyProperty = clr.GetProperty("PartitionKey");
                if (partitionKeyProperty is null || partitionKeyProperty.PropertyType != typeof(string))
                {
                    throw new InvalidOperationException(
                        $"Entity '{clr.Name}' implements ICosmosPartitionKey but has no public string PartitionKey property.");
                }

                builder.HasPartitionKey("PartitionKey");
            }

            foreach (var property in entityType.GetProperties())
            {
                var original = property.Name;
                var camelCase = char.ToLowerInvariant(original[0]) + original[1..];

                Microsoft.EntityFrameworkCore.CosmosPropertyExtensions
                    .SetJsonPropertyName(property, camelCase);
            }
        }

        return modelBuilder;
    }
}
