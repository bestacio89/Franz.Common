using System;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Hosting.NoOp;

public sealed class NoOpGrpcMetrics : IFranzGrpcMetrics
{
    public void RecordRequest(GrpcMetricContext context) { }

    public void RecordSuccess(GrpcMetricContext context, long elapsedMs) { }

    public void RecordFailure(GrpcMetricContext context, Exception exception, long elapsedMs) { }
}
