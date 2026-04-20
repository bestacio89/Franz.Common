using Franz.Common.AzureCosmosDB.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Franz.Common.AzureCosmosDB.Options;

public sealed class CosmosOptions
{
  /// <summary>
  /// Cosmos DB Account endpoint (URI).
  /// </summary>
  [Required(AllowEmptyStrings = false)]
  public string AccountEndpoint { get; set; } = default!;

  /// <summary>
  /// Cosmos DB primary or secondary key.
  /// </summary>
  [Required(AllowEmptyStrings = false)]
  public string AccountKey { get; set; } = default!;

  /// <summary>
  /// Name of the database to use.
  /// </summary>
  [Required(AllowEmptyStrings = false)]
  public string DatabaseName { get; set; } = default!;

  /// <summary>
  /// Enable/disable Cosmos integration.
  /// Useful for environments where Cosmos is not required.
  /// </summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Optional application name for diagnostics.
  /// </summary>
  public string? ApplicationName { get; set; }

  /// <summary>
  /// Default throughput on database creation (RU/s).
  /// </summary>
  [Range(400, 1000000)]
  public int? DatabaseThroughput { get; set; }

  public CosmosMessagingOptions Messaging { get; set; } = new();
}