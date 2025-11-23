using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Pipelines;

/// <summary>
/// Helper for composing the Franz gRPC server behavior pipeline.
/// Given an ordered list of server behaviors and a terminal delegate (the actual service method),
/// it builds the final executable pipeline.
/// </summary>
public static class FranzServerInteropPipeline
{
  /// <summary>
  /// Builds a server-side behavior pipeline around a terminal delegate.
  /// Behaviors are applied in the order of the array, outermost first.
  /// </summary>
  /// <typeparam name="TRequest">The incoming gRPC request type.</typeparam>
  /// <typeparam name="TResponse">The outgoing gRPC response type.</typeparam>
  /// <param name="behaviors">Ordered list of server behaviors.</param>
  /// <param name="terminal">
  /// The final delegate that actually invokes the service method implementation.
  /// </param>
  public static GrpcServerPipelineDelegate<TRequest, TResponse> Build<TRequest, TResponse>(
      IGrpcServerBehavior<TRequest, TResponse>[] behaviors,
      GrpcServerPipelineDelegate<TRequest, TResponse> terminal)
      where TRequest : class
      where TResponse : class
  {
    if (behaviors is null)
      throw new ArgumentNullException(nameof(behaviors));
    if (terminal is null)
      throw new ArgumentNullException(nameof(terminal));

    var pipeline = terminal;

    // Apply behaviors in reverse order so behaviors[0] becomes the outermost wrapper.
    for (int i = behaviors.Length - 1; i >= 0; i--)
    {
      var behavior = behaviors[i]
                     ?? throw new InvalidOperationException($"Server behavior at index {i} is null.");

      var next = pipeline;

      pipeline = async (request, context, cancellationToken) =>
      {
        return await behavior
            .InvokeAsync(request, context, next, cancellationToken)
            .ConfigureAwait(false);
      };
    }

    return pipeline;
  }
}
