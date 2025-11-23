using System;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Hosting.NoOp;

public sealed class NoOpGrpcMetrics : IFranzGrpcMetrics
{
  public void RecordRequest(GrpcCallContext context) { }

  public void RecordSuccess(GrpcCallContext context, long elapsedMs) { }

  public void RecordFailure(GrpcCallContext context, Exception ex, long elapsedMs) { }
}
