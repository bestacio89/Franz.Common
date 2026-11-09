using System;

namespace Franz.Common.Grpc.Configuration;

/// <summary>
/// Configuration for calls executed through the Franz gRPC client pipeline.
/// Transport retries, metadata credentials, and payload logging remain explicit
/// host concerns and are not controlled by these options.
/// </summary>
public sealed record FranzGrpcClientOptions
{
    /// <summary>
    /// Default deadline duration for unary calls when no explicit deadline is
    /// supplied. Must be greater than zero and no longer than 365 days.
    /// </summary>
    public TimeSpan DefaultClientTimeout { get; init; } = TimeSpan.FromSeconds(15);
}
