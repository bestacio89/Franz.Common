using System;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Abstraction over logging strategy for Franz gRPC requests.
/// Allows plugging any structured logger (Serilog, MS ILogger, OTel, etc.).
/// </summary>
public interface IFranzGrpcLogger
{
  IDisposable BeginScope(GrpcCallContext context);

  void LogRequest<TRequest>(GrpcCallContext context, TRequest request);

  void LogResponse<TResponse>(GrpcCallContext context, TResponse response, long elapsedMs);

  void LogError(GrpcCallContext context, Exception ex, long elapsedMs);

  /// <summary>Records a streaming RPC start without logging message payloads.</summary>
  void LogStreamStarted(GrpcCallContext context, string rpcShape);

  /// <summary>Records normal completion of a streaming RPC.</summary>
  void LogStreamCompleted(GrpcCallContext context, string rpcShape, long elapsedMs);

  /// <summary>Records a streaming RPC failure without exposing message payloads.</summary>
  void LogStreamError(GrpcCallContext context, string rpcShape, Exception exception, long elapsedMs);

  /// <summary>Records early disposal of a response stream before normal completion.</summary>
  void LogStreamAborted(GrpcCallContext context, string rpcShape, long elapsedMs);
}
