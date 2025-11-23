using System;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Hosting.NoOp;

public sealed class NoOpGrpcLogger : IFranzGrpcLogger
{
  private sealed class NoScope : IDisposable
  {
    public void Dispose() { }
  }

  public IDisposable BeginScope(GrpcCallContext context)
      => new NoScope();

  public void LogRequest<TRequest>(GrpcCallContext context, TRequest request) { }

  public void LogResponse<TResponse>(GrpcCallContext context, TResponse response, long elapsedMs) { }

  public void LogError(GrpcCallContext context, Exception ex, long elapsedMs) { }
}
