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
}
