using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server.Pipelines;

namespace Franz.Common.Grpc.Server;

/// <summary>
/// Base class for all Franz gRPC services.
/// Wraps the handler logic with the full Franz server pipeline: 
/// Validation → Tenant → Authorization → Logging → Metrics → Exception mapping.
/// </summary>
public abstract class FranzServiceBase
{
  private readonly IGrpcServerBehaviorProvider _behaviorProvider;

  protected FranzServiceBase(IGrpcServerBehaviorProvider behaviorProvider)
  {
    _behaviorProvider = behaviorProvider
                        ?? throw new ArgumentNullException(nameof(behaviorProvider));
  }

  /// <summary>
  /// Executes a unary gRPC service method through the Franz pipeline.
  /// </summary>
  protected async Task<TResponse> ExecuteUnary<TRequest, TResponse>(
      TRequest request,
      ServerCallContext callContext,
      Func<TRequest, Task<TResponse>> handler)
      where TRequest : class
      where TResponse : class
  {
    if (request is null)
      throw new ArgumentNullException(nameof(request));
    if (callContext is null)
      throw new ArgumentNullException(nameof(callContext));
    if (handler is null)
      throw new ArgumentNullException(nameof(handler));

    // Create Franz context from gRPC ServerCallContext
    var context = GrpcCallContext.FromServerCallContext(callContext);

    // Token from gRPC runtime
    var cancellationToken = callContext.CancellationToken;

    // Final delegate: the actual service method implementation
    async Task<TResponse> Terminal(
        TRequest req,
        GrpcCallContext ctx,
        CancellationToken token)
    {
      return await handler(req).ConfigureAwait(false);
    }

    // Resolve ordered server behaviors for this request/response pair
    var behaviors = _behaviorProvider
        .ResolveBehaviors<TRequest, TResponse>();

    // Build the pipeline (outer → inner)
    var pipeline = FranzServerInteropPipeline
        .Build(behaviors, Terminal);

    // Execute pipeline
    return await pipeline(request, context, cancellationToken)
        .ConfigureAwait(false);
  }
}
