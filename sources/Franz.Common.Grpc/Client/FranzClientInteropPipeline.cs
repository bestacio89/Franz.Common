using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Pipelines;

/// <summary>
/// Helper for composing the Franz gRPC client behavior pipeline.
/// Given an ordered list of behaviors and a terminal delegate (the actual transport call),
/// it builds the final executable pipeline.
/// </summary>
public static class FranzClientInteropPipeline
{
  /// <summary>
  /// Builds a client-side behavior pipeline around a terminal delegate.
  /// Behaviors are applied in the order of the array, outermost first.
  /// </summary>
  /// <typeparam name="TRequest">Request message type.</typeparam>
  /// <typeparam name="TResponse">Response message type.</typeparam>
  /// <param name="behaviors">Ordered list of behaviors.</param>
  /// <param name="terminal">
  /// The final delegate that actually performs the gRPC transport call
  /// (usually wrapping the generated gRPC client).
  /// </param>
  public static GrpcClientPipelineDelegate<TRequest, TResponse> Build<TRequest, TResponse>(
      IGrpcClientBehavior<TRequest, TResponse>[] behaviors,
      GrpcClientPipelineDelegate<TRequest, TResponse> terminal)
      where TRequest : class
      where TResponse : class
  {
    if (behaviors is null)
      throw new ArgumentNullException(nameof(behaviors));

    if (terminal is null)
      throw new ArgumentNullException(nameof(terminal));

    var pipeline = terminal;

    // Apply behaviors in reverse so that behaviors[0] becomes the outermost
    for (int i = behaviors.Length - 1; i >= 0; i--)
    {
      var behavior = behaviors[i] ?? throw new InvalidOperationException(
          $"Client behavior at index {i} is null.");

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
