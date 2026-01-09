using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cosmos;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Franz.Common.AzureCosmosDB.Conventions;

/// <summary>
/// Applies Cosmos-specific model conventions:
///  - Automatic container naming
///  - Automatic partition key mapping via ICosmosPartitionKey
///  - JSON camelCase mapping
///
/// These conventions are only active when the DbContext is configured with UseCosmos().
/// </summary>
public static class CosmosModelBuilderExtensions
{
  public static ModelBuilder ApplyCosmosConventions(this ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      var clr = entityType.ClrType;

      // Ignore EF internal/clr system types
      if (clr == null || clr.Namespace?.StartsWith("Microsoft") == true)
        continue;

      // Builder for the concrete entity
      var builder = modelBuilder.Entity(clr);

      // 1️⃣ Container naming convention
      // Container = lowercase class name (e.g. Order -> "order")
      builder.ToContainer(clr.Name.ToLowerInvariant());

      // 2️⃣ PartitionKey convention via marker interface
      if (typeof(ICosmosPartitionKey).IsAssignableFrom(clr))
      {
        var pkProp = clr.GetProperty("PartitionKey");
        if (pkProp == null)
        {
          throw new InvalidOperationException(
              $"Entity '{clr.Name}' implements ICosmosPartitionKey but has no PartitionKey property. " +
              "Add a public string PartitionKey property to this entity.");
        }

        builder.HasPartitionKey("PartitionKey");
      }

      // 3️⃣ JSON camel-case mapping for Cosmos documents
      foreach (var prop in entityType.GetProperties())
      {
        var original = prop.Name;
        var camelCase = char.ToLowerInvariant(original[0]) + original.Substring(1);

        // Fully-qualified method call due to EF 10 ambiguous extension methods
        Microsoft.EntityFrameworkCore.CosmosPropertyExtensions
            .SetJsonPropertyName(prop, camelCase);
      }
    }

    return modelBuilder;
  }
}
