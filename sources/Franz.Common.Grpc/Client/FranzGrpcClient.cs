using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client;

/// <summary>
/// Convenience base class for strongly-typed Franz gRPC clients.
/// It fixes the service name and exposes helpers for unary calls.
/// </summary>
/// <typeparam name="TInnerClient">
/// The generated gRPC client type (inherits from <see cref="ClientBase{TInnerClient}"/>).
/// </typeparam>
public abstract class FranzGrpcClient<TInnerClient> : FranzGrpcClientBase<TInnerClient>
    where TInnerClient : ClientBase<TInnerClient>
{
  /// <summary>
  /// Fully-qualified gRPC service name (e.g. "package.Service").
  /// </summary>
  protected string ServiceName { get; }

  protected FranzGrpcClient(
      TInnerClient innerClient,
      IGrpcClientBehaviorProvider behaviorProvider,
      string serviceName)
      : base(innerClient, behaviorProvider)
  {
    ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
  }

  /// <summary>
  /// Helper for unary RPC calls that automatically uses the configured <see cref="ServiceName"/>.
  /// </summary>
  protected Task<TResponse> UnaryAsync<TRequest, TResponse>(
      TRequest request,
      string methodName,
      Func<TInnerClient, TRequest, CallOptions, Task<TResponse>> transportFunc,
      Action<Metadata>? metadataModifier = null,
      string? tenantId = null,
      string? userId = null,
      CancellationToken cancellationToken = default)
      where TRequest : class
      where TResponse : class
  {
    if (string.IsNullOrWhiteSpace(methodName))
      throw new ArgumentException("Method name must be provided.", nameof(methodName));

    return ExecuteUnaryAsync(
        request,
        ServiceName,
        methodName,
        transportFunc,
        metadataModifier,
        tenantId,
        userId,
        cancellationToken);
  }
}
