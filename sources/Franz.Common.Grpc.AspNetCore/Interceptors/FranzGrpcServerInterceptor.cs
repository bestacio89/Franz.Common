using System.Runtime.CompilerServices;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.AspNetCore.Configuration;
using Franz.Common.Grpc.Server.Pipelines;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Grpc.AspNetCore.Interceptors;

/// <summary>
/// Applies the canonical Franz server pipelines at the ASP.NET Core gRPC transport
/// boundary. Each asynchronous scope remains alive for the complete RPC lifetime,
/// including request and response stream enumeration.
/// </summary>
public sealed class FranzGrpcServerInterceptor : Interceptor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly FranzGrpcAspNetCoreOptions _options;

    public FranzGrpcServerInterceptor(
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<FranzGrpcAspNetCoreOptions> options)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IGrpcServerBehaviorProvider>();
        var callContext = CreateCallContext(context);
        var pipeline = FranzServerInteropPipeline.Build(
            provider.ResolveBehaviors<TRequest, TResponse>(),
            (message, finalContext, _) =>
            {
                GrpcCallContext.EstablishServerCallContext(context, finalContext);
                return continuation(message, context);
            });

        return await pipeline(request, callContext, context.CancellationToken).ConfigureAwait(false);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IGrpcServerStreamingBehaviorProvider>();
        var callContext = CreateCallContext(context);
        var pipeline = GrpcServerStreamingInteropPipeline.BuildServerStreaming(
            provider.ResolveBehaviors<TRequest, TResponse>(),
            (message, finalContext, cancellationToken) =>
                ExecuteServerStreamingAsync(message, finalContext, context, continuation, cancellationToken));

        await foreach (var response in pipeline(request, callContext, context.CancellationToken)
            .WithCancellation(context.CancellationToken)
            .ConfigureAwait(false))
        {
            await responseStream.WriteAsync(response).ConfigureAwait(false);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IGrpcClientStreamingBehaviorProvider>();
        var callContext = CreateCallContext(context);
        var requestEnumerable = new GrpcAsyncStreamReaderEnumerable<TRequest>(requestStream);
        var pipeline = GrpcServerStreamingInteropPipeline.BuildClientStreaming(
            provider.ResolveBehaviors<TRequest, TResponse>(),
            (messages, finalContext, cancellationToken) =>
                ExecuteClientStreamingAsync(messages, finalContext, context, continuation, cancellationToken));

        return await pipeline(requestEnumerable, callContext, context.CancellationToken).ConfigureAwait(false);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IGrpcDuplexStreamingBehaviorProvider>();
        var callContext = CreateCallContext(context);
        var requestEnumerable = new GrpcAsyncStreamReaderEnumerable<TRequest>(requestStream);
        var pipeline = GrpcServerStreamingInteropPipeline.BuildDuplexStreaming(
            provider.ResolveBehaviors<TRequest, TResponse>(),
            (messages, finalContext, cancellationToken) =>
                ExecuteDuplexStreamingAsync(messages, finalContext, context, continuation, cancellationToken));

        await foreach (var response in pipeline(requestEnumerable, callContext, context.CancellationToken)
            .WithCancellation(context.CancellationToken)
            .ConfigureAwait(false))
        {
            await responseStream.WriteAsync(response).ConfigureAwait(false);
        }
    }

    private static async Task<TResponse> ExecuteClientStreamingAsync<TRequest, TResponse>(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext callContext,
        ServerCallContext serverCallContext,
        ClientStreamingServerMethod<TRequest, TResponse> continuation,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        GrpcCallContext.EstablishServerCallContext(serverCallContext, callContext);

        await using var requestReader = new GrpcAsyncEnumerableStreamReader<TRequest>(
            requestStream,
            cancellationToken);

        return await continuation(requestReader, serverCallContext).ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<TResponse> ExecuteServerStreamingAsync<TRequest, TResponse>(
        TRequest request,
        GrpcCallContext callContext,
        ServerCallContext serverCallContext,
        ServerStreamingServerMethod<TRequest, TResponse> continuation,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            serverCallContext.CancellationToken);
        var bridge = new GrpcServerStreamBridge<TResponse>(linkedCancellation.Token);
        var producer = ProduceAsync();

        try
        {
            await foreach (var response in bridge.ReadAllAsync(linkedCancellation.Token).ConfigureAwait(false))
            {
                yield return response;
            }

            await producer.ConfigureAwait(false);
        }
        finally
        {
            linkedCancellation.Cancel();
            await producer.ConfigureAwait(false);
        }

        async Task ProduceAsync()
        {
            try
            {
                GrpcCallContext.EstablishServerCallContext(serverCallContext, callContext);
                await continuation(request, bridge, serverCallContext).ConfigureAwait(false);
                bridge.Complete();
            }
            catch (Exception exception)
            {
                bridge.Complete(exception);
            }
        }
    }

    private static async IAsyncEnumerable<TResponse> ExecuteDuplexStreamingAsync<TRequest, TResponse>(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext callContext,
        ServerCallContext serverCallContext,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            serverCallContext.CancellationToken);
        var bridge = new GrpcServerStreamBridge<TResponse>(linkedCancellation.Token);
        var producer = ProduceAsync();

        try
        {
            await foreach (var response in bridge.ReadAllAsync(linkedCancellation.Token).ConfigureAwait(false))
            {
                yield return response;
            }

            await producer.ConfigureAwait(false);
        }
        finally
        {
            linkedCancellation.Cancel();
            await producer.ConfigureAwait(false);
        }

        async Task ProduceAsync()
        {
            try
            {
                GrpcCallContext.EstablishServerCallContext(serverCallContext, callContext);
                await using var requestReader = new GrpcAsyncEnumerableStreamReader<TRequest>(
                    requestStream,
                    linkedCancellation.Token);
                await continuation(requestReader, bridge, serverCallContext).ConfigureAwait(false);
                bridge.Complete();
            }
            catch (Exception exception)
            {
                bridge.Complete(exception);
            }
        }
    }

    private GrpcCallContext CreateCallContext(ServerCallContext serverCallContext)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var userId = principal?.Identities
            .Where(identity => identity.IsAuthenticated)
            .Select(identity => identity.FindFirst(_options.UserIdClaimType)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return GrpcCallContext.FromServerCallContext(serverCallContext, userId: userId);
    }
}
