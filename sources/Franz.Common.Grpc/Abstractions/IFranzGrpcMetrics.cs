using System;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Host-selected gRPC metrics abstraction. Implementations receive only bounded RPC
/// dimensions; tenant, user, correlation, peer, and request identifiers are excluded.
/// </summary>
public interface IFranzGrpcMetrics
{
    void RecordRequest(GrpcMetricContext context);

    void RecordSuccess(GrpcMetricContext context, long elapsedMs);

    void RecordFailure(GrpcMetricContext context, Exception exception, long elapsedMs);
}
