using System;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Resolves the ordered list of server behaviors for the given gRPC request/response types.
/// The provider implementation is responsible for ordering the behaviors correctly
/// according to the Franz canonical pipeline.
/// </summary>
public interface IGrpcServerBehaviorProvider
{
  /// <summary>
  /// Resolves the ordered list of behaviors for server-side execution.
  /// </summary>
  /// <typeparam name="TRequest">Incoming request type.</typeparam>
  /// <typeparam name="TResponse">Outgoing response type.</typeparam>
  /// <returns>
  /// An ordered array of server behaviors,
  /// where index 0 is the first (outermost) behavior to be executed.
  /// </returns>
  IGrpcServerBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
      where TRequest : class
      where TResponse : class;
}
