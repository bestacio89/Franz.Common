using System;

namespace Franz.Common.Grpc.Configuration;

/// <summary>
/// Configuration options for the Franz gRPC client pipeline.
/// </summary>
public sealed record FranzGrpcClientOptions
{
  /// <summary>
  /// The fully-qualified service name (package.Service).
  /// Used to populate GrpcCallContext.ServiceName on outgoing calls.
  /// </summary>
  public string? ServiceName { get; init; }

  /// <summary>
  /// Optional default deadline for calls if no timeout exists on context.
  /// Default: 15 seconds.
  /// </summary>
  public TimeSpan DefaultClientTimeout { get; init; } = TimeSpan.FromSeconds(15);

  /// <summary>
  /// Whether to attach default metadata (correlation ID, request ID, tenant, user).
  /// Default: true.
  /// </summary>
  public bool AutoInjectMetadata { get; init; } = true;

  /// <summary>
  /// When true, request payloads will be logged on the client side.
  /// Default: false (safety).
  /// </summary>
  public bool LogRequestPayload { get; init; } = false;

  /// <summary>
  /// When true, response payloads will be logged on the client side.
  /// Default: false.
  /// </summary>
  public bool LogResponsePayload { get; init; } = false;

  /// <summary>
  /// Enables exponential retry for transient network errors.
  /// Default: false.
  /// </summary>
  public bool EnableRetries { get; init; } = false;

  /// <summary>
  /// Number of retry attempts when EnableRetries = true.
  /// Default: 3 attempts.
  /// </summary>
  public int RetryCount { get; init; } = 3;

  /// <summary>
  /// Base delay (ms) for exponential backoff.
  /// Default: 200 ms.
  /// </summary>
  public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(200);

  /// <summary>
  /// Whether to resolve the method name automatically for the context.
  /// Default: true.
  /// </summary>
  public bool AutoResolveMethodName { get; init; } = true;

  /// <summary>
  /// Named gRPC services and their connection information.
  /// Used by FranzGrpcClientFactory to construct channels.
  /// </summary>
  public Dictionary<string, FranzGrpcClientServiceConfig> Services { get; init; }
      = new(StringComparer.OrdinalIgnoreCase);

}
