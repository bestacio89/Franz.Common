using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Configuration;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Franz.Common.Grpc.Abstractions;

public abstract class FranzGrpcClientBase<TClient>
    where TClient : ClientBase<TClient>
{
  protected TClient GrpcClient { get; }

  private readonly IGrpcClientBehaviorProvider _behaviorProvider;
  private readonly FranzGrpcClientOptions _options;

  protected FranzGrpcClientBase(
      TClient grpcClient,
      IGrpcClientBehaviorProvider behaviorProvider,
      IOptions<FranzGrpcClientOptions>? options = null)
  {
    GrpcClient = grpcClient ?? throw new ArgumentNullException(nameof(grpcClient));
    _behaviorProvider = behaviorProvider ?? throw new ArgumentNullException(nameof(behaviorProvider));
    _options = options?.Value ?? new FranzGrpcClientOptions();
  }

  /// <summary>
  /// Executes a unary RPC through the configured Franz client behavior pipeline.
  /// An explicit deadline takes precedence over the configured default timeout.
  /// </summary>
  protected async Task<TResponse> ExecuteUnaryAsync<TRequest, TResponse>(
      TRequest request,
      string serviceName,
      string methodName,
      Func<TClient, TRequest, CallOptions, Task<TResponse>> transportFunc,
      Action<Metadata>? metadataModifier = null,
      string? tenantId = null,
      string? userId = null,
      CancellationToken cancellationToken = default,
      DateTime? deadlineUtc = null)
      where TRequest : class
      where TResponse : class
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(transportFunc);

    var effectiveDeadline = deadlineUtc
        ?? DateTime.UtcNow.Add(_options.DefaultClientTimeout);

    var context = GrpcCallContext.CreateClient(
        serviceName,
        methodName,
        tenantId: tenantId,
        userId: userId,
        deadlineUtc: effectiveDeadline,
        cancellationToken: cancellationToken);

    if (metadataModifier is not null)
    {
      var temp = context.ToRequestMetadata();
      metadataModifier(temp);

      foreach (var entry in temp)
        context = context.WithAdditionalHeader(entry.Key, entry.Value);
    }

    async Task<TResponse> FinalTransport(
        TRequest req,
        GrpcCallContext ctx,
        CancellationToken token)
    {
      var options = ctx.ToCallOptions();
      return await transportFunc(GrpcClient, req, options).ConfigureAwait(false);
    }

    var pipeline = BuildClientPipeline(
        FinalTransport,
        _behaviorProvider.ResolveBehaviors<TRequest, TResponse>());

    return await pipeline(request, context, cancellationToken).ConfigureAwait(false);
  }

  private static GrpcClientPipelineDelegate<TRequest, TResponse> BuildClientPipeline<TRequest, TResponse>(
      GrpcClientPipelineDelegate<TRequest, TResponse> terminal,
      IGrpcClientBehavior<TRequest, TResponse>[] behaviors)
      where TRequest : class
      where TResponse : class
  {
    var pipeline = terminal;

    for (var i = behaviors.Length - 1; i >= 0; i--)
    {
      var behavior = behaviors[i]
          ?? throw new InvalidOperationException($"Client behavior at index {i} is null.");
      var next = pipeline;

      pipeline = async (req, ctx, token) =>
          await behavior.InvokeAsync(req, ctx, next, token).ConfigureAwait(false);
    }

    return pipeline;
  }
}
