using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Franz.Common.Grpc.Abstractions;

public abstract class FranzGrpcClientBase<TClient>
    where TClient : ClientBase<TClient>
{
  protected TClient GrpcClient { get; }

  private readonly IGrpcClientBehaviorProvider _behaviorProvider;

  protected FranzGrpcClientBase(
      TClient grpcClient,
      IGrpcClientBehaviorProvider behaviorProvider)
  {
    GrpcClient = grpcClient ?? throw new ArgumentNullException(nameof(grpcClient));
    _behaviorProvider = behaviorProvider ?? throw new ArgumentNullException(nameof(behaviorProvider));
  }

  // =====================================================================
  // UNARY CALL EXECUTION
  // =====================================================================

  protected async Task<TResponse> ExecuteUnaryAsync<TRequest, TResponse>(
      TRequest request,
      string serviceName,
      string methodName,
      Func<TClient, TRequest, CallOptions, Task<TResponse>> transportFunc,
      Action<Metadata>? metadataModifier = null,
      string? tenantId = null,
      string? userId = null,
      CancellationToken cancellationToken = default)
      where TRequest : class
      where TResponse : class
  {
    if (request is null)
      throw new ArgumentNullException(nameof(request));

    // Build logical context
    var context = GrpcCallContext.CreateClient(
        serviceName,
        methodName,
        tenantId: tenantId,
        userId: userId,
        cancellationToken: cancellationToken
    );

    // Allow external metadata injection
    if (metadataModifier is not null)
    {
      var temp = context.ToRequestMetadata();
      metadataModifier(temp);

      foreach (var entry in temp)
        context = context.WithAdditionalHeader(entry.Key, entry.Value);
    }

    // Prepare transport layer delegate
    async Task<TResponse> FinalTransport(
        TRequest req,
        GrpcCallContext ctx,
        CancellationToken token)
    {
      var options = ctx.ToCallOptions();
      return await transportFunc(GrpcClient, req, options).ConfigureAwait(false);
    }

    // Build pipeline using resolved behaviors
    var pipeline = BuildClientPipeline(
        FinalTransport,
        _behaviorProvider.ResolveBehaviors<TRequest, TResponse>());

    return await pipeline(request, context, cancellationToken).ConfigureAwait(false);
  }

  // =====================================================================
  // PIPELINE BUILDING
  // =====================================================================

  private static GrpcClientPipelineDelegate<TRequest, TResponse> BuildClientPipeline<TRequest, TResponse>(
      GrpcClientPipelineDelegate<TRequest, TResponse> terminal,
      IGrpcClientBehavior<TRequest, TResponse>[] behaviors)
      where TRequest : class
      where TResponse : class
  {
    var pipeline = terminal;

    // Reverse order to wrap correctly
    for (int i = behaviors.Length - 1; i >= 0; i--)
    {
      var behavior = behaviors[i];
      var next = pipeline;

      pipeline = async (req, ctx, token) =>
          await behavior.InvokeAsync(req, ctx, next, token).ConfigureAwait(false);
    }

    return pipeline;
  }
}
