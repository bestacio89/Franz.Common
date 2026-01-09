namespace Franz.Common.AzureCosmosDB.Options;

public sealed class CosmosOptions
{
  /// <summary>
  /// Cosmos DB Account endpoint (URI).
  /// </summary>
  public string AccountEndpoint { get; set; } = default!;

  /// <summary>
  /// Cosmos DB primary or secondary key.
  /// </summary>
  public string AccountKey { get; set; } = default!;

  /// <summary>
  /// Name of the database to use.
  /// </summary>
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
  /// Default throughput on database creation.
  /// </summary>
  public bool? DatabaseThroughput { get; set; }
}
