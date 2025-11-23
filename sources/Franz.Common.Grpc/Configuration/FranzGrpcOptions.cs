using System;

namespace Franz.Common.Grpc.Configuration;

/// <summary>
/// Global configuration options for the Franz gRPC transport. 
/// These affect both client and server behavior.
/// </summary>
public sealed record FranzGrpcOptions
{
  /// <summary>
  /// When true, correlation IDs are always generated if not provided by the caller.
  /// Default: true.
  /// </summary>
  public bool AutoGenerateCorrelationId { get; init; } = true;

  /// <summary>
  /// Default timeout for server or client calls when no deadline is present.
  /// Default: 30 seconds.
  /// </summary>
  public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);

  /// <summary>
  /// Whether metadata keys should be normalized to lowercase for consistency.
  /// Default: true.
  /// </summary>
  public bool NormalizeMetadataKeys { get; init; } = true;

  /// <summary>
  /// When true, tenant/user metadata is automatically propagated if present.
  /// Default: true.
  /// </summary>
  public bool PropagateIdentity { get; init; } = true;

  /// <summary>
  /// Controls whether request/response bodies can be logged by LoggingServerBehavior.
  /// Default: false (safe default).
  /// </summary>
  public bool EnablePayloadLogging { get; init; } = false;

  /// <summary>
  /// Optional prefix for all metadata keys applied by Franz (e.g. x-franz-).
  /// Default: "x-franz-".
  /// </summary>
  public string MetadataKeyPrefix { get; init; } = "x-franz-";
}
