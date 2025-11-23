namespace Franz.Common.Grpc.Configuration;

public sealed record FranzGrpcClientServiceConfig
{
  /// <summary>
  /// Base address of the gRPC service (e.g., https://users.api:5001).
  /// </summary>
  public required string BaseAddress { get; init; }

  /// <summary>
  /// Optional deadline override for this specific service.
  /// If null, FranzGrpcClientOptions.DefaultClientTimeout is used.
  /// </summary>
  public TimeSpan? Timeout { get; init; }

  /// <summary>
  /// Enable/disable metadata injection at the service level.
  /// Default: null = use global setting.
  /// </summary>
  public bool? AutoInjectMetadata { get; init; }
}
