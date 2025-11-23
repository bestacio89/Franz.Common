using System;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Abstraction for collecting metrics for Franz gRPC server calls.
/// Implement using OpenTelemetry, Prometheus, or any other provider.
/// </summary>
public interface IFranzGrpcMetrics
{
  /// <summary>Increment a counter for this request.</summary>
  void RecordRequest(GrpcCallContext context);

  /// <summary>Record success execution duration in milliseconds.</summary>
  void RecordSuccess(GrpcCallContext context, long elapsedMs);

  /// <summary>Record failure execution duration in milliseconds.</summary>
  void RecordFailure(GrpcCallContext context, Exception ex, long elapsedMs);
}
