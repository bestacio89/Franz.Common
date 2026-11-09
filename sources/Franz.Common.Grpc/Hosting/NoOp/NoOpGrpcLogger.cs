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

  public void LogStreamStarted(GrpcCallContext context, string rpcShape) { }

  public void LogStreamCompleted(GrpcCallContext context, string rpcShape, long elapsedMs) { }

  public void LogStreamError(GrpcCallContext context, string rpcShape, Exception exception, long elapsedMs) { }

  public void LogStreamAborted(GrpcCallContext context, string rpcShape, long elapsedMs) { }
}
