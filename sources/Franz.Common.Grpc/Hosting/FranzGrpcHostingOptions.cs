namespace Franz.Common.Grpc.Hosting;

/// <summary>
/// Placeholder for future hosting-wide configuration applicable
/// to any Franz gRPC hosting environment.
/// 
/// This avoids introducing ASP.NET Core dependencies in the core package.
/// </summary>
public sealed record FranzGrpcHostingOptions
{
  /// <summary>
  /// Whether to automatically apply Franz gRPC naming conventions
  /// when exposing services in a hosting environment.
  /// Default: true.
  /// </summary>
  public bool ApplyNamingConventions { get; init; } = true;
}
