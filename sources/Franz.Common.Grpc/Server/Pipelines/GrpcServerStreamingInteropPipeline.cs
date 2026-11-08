using System;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Pipelines;

/// <summary>
/// Composes the three streaming server RPC shapes without collapsing their
/// distinct request/response lifetimes into the unary pipeline.
/// Behaviors are applied in array order, outermost first.
/// </summary>
public static class GrpcServerStreamingInteropPipeline
{
    public static GrpcServerStreamingPipelineDelegate<TRequest, TResponse> BuildServerStreaming<TRequest, TResponse>(
        IGrpcServerStreamingBehavior<TRequest, TResponse>[] behaviors,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> terminal)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(terminal);

        var pipeline = terminal;
        for (var index = behaviors.Length - 1; index >= 0; index--)
        {
            var behavior = behaviors[index]
                ?? throw new InvalidOperationException($"The server-streaming behavior at index {index} is null.");
            var next = pipeline;
            pipeline = (request, context, cancellationToken) =>
                behavior.InvokeAsync(request, context, next, cancellationToken);
        }

        return pipeline;
    }

    public static GrpcClientStreamingPipelineDelegate<TRequest, TResponse> BuildClientStreaming<TRequest, TResponse>(
        IGrpcClientStreamingBehavior<TRequest, TResponse>[] behaviors,
        GrpcClientStreamingPipelineDelegate<TRequest, TResponse> terminal)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(terminal);

        var pipeline = terminal;
        for (var index = behaviors.Length - 1; index >= 0; index--)
        {
            var behavior = behaviors[index]
                ?? throw new InvalidOperationException($"The client-streaming behavior at index {index} is null.");
            var next = pipeline;
            pipeline = (requestStream, context, cancellationToken) =>
                behavior.InvokeAsync(requestStream, context, next, cancellationToken);
        }

        return pipeline;
    }

    public static GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> BuildDuplexStreaming<TRequest, TResponse>(
        IGrpcDuplexStreamingBehavior<TRequest, TResponse>[] behaviors,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> terminal)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(terminal);

        var pipeline = terminal;
        for (var index = behaviors.Length - 1; index >= 0; index--)
        {
            var behavior = behaviors[index]
                ?? throw new InvalidOperationException($"The duplex-streaming behavior at index {index} is null.");
            var next = pipeline;
            pipeline = (requestStream, context, cancellationToken) =>
                behavior.InvokeAsync(requestStream, context, next, cancellationToken);
        }

        return pipeline;
    }
}
