using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server.Pipelines;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcStreamingPipelineTests
{
    [Fact]
    public async Task ServerStreaming_ComposesBehaviorsOutermostFirstAndPreservesEnumeration()
    {
        var order = new List<string>();
        var behaviors = new IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>[]
        {
            new ServerStreamingBehavior("outer", order),
            new ServerStreamingBehavior("inner", order)
        };

        var pipeline = GrpcServerStreamingInteropPipeline.BuildServerStreaming(
            behaviors,
            (request, _, _) => Echo(request));

        var values = new List<string>();
        await foreach (var item in pipeline(new ProbeMessage("request"), GrpcCallContext.CreateClient("tests.Service", "Stream"), CancellationToken.None))
            values.Add(item.Value);

        Assert.Equal(new[] { "outer:before", "inner:before", "inner:after", "outer:after" }, order);
        Assert.Equal(new[] { "request" }, values);
    }

    [Fact]
    public async Task ClientStreaming_ComposesBehaviorsAroundCompleteRequestConsumption()
    {
        var order = new List<string>();
        var behaviors = new IGrpcClientStreamingBehavior<ProbeMessage, ProbeMessage>[]
        {
            new ClientStreamingBehavior("outer", order),
            new ClientStreamingBehavior("inner", order)
        };

        var pipeline = GrpcServerStreamingInteropPipeline.BuildClientStreaming(
            behaviors,
            async (requestStream, _, token) =>
            {
                var values = new List<string>();
                await foreach (var item in requestStream.WithCancellation(token))
                    values.Add(item.Value);
                return new ProbeMessage(string.Join(",", values));
            });

        var response = await pipeline(Values("one", "two"), GrpcCallContext.CreateClient("tests.Service", "Upload"), CancellationToken.None);

        Assert.Equal("one,two", response.Value);
        Assert.Equal(new[] { "outer:before", "inner:before", "inner:after", "outer:after" }, order);
    }

    [Fact]
    public async Task DuplexStreaming_ComposesBehaviorsAndPreservesRequestAndResponseStreams()
    {
        var order = new List<string>();
        var behaviors = new IGrpcDuplexStreamingBehavior<ProbeMessage, ProbeMessage>[]
        {
            new DuplexStreamingBehavior("outer", order),
            new DuplexStreamingBehavior("inner", order)
        };

        var pipeline = GrpcServerStreamingInteropPipeline.BuildDuplexStreaming(
            behaviors,
            (requestStream, _, token) => EchoAll(requestStream, token));

        var values = new List<string>();
        await foreach (var item in pipeline(Values("one", "two"), GrpcCallContext.CreateClient("tests.Service", "Duplex"), CancellationToken.None))
            values.Add(item.Value);

        Assert.Equal(new[] { "one", "two" }, values);
        Assert.Equal(new[] { "outer:before", "inner:before", "inner:after", "outer:after" }, order);
    }

    [Fact]
    public void Builders_RejectNullBehaviorArraysAndTerminals()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GrpcServerStreamingInteropPipeline.BuildServerStreaming<ProbeMessage, ProbeMessage>(null!, (_, _, _) => Echo(new ProbeMessage("x"))));
        Assert.Throws<ArgumentNullException>(() =>
            GrpcServerStreamingInteropPipeline.BuildClientStreaming<ProbeMessage, ProbeMessage>([], null!));
        Assert.Throws<ArgumentNullException>(() =>
            GrpcServerStreamingInteropPipeline.BuildDuplexStreaming<ProbeMessage, ProbeMessage>(null!, (_, _, _) => Echo(new ProbeMessage("x"))));
    }

    private static async IAsyncEnumerable<ProbeMessage> Echo(
        ProbeMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return message;
    }

    private static async IAsyncEnumerable<ProbeMessage> EchoAll(
        IAsyncEnumerable<ProbeMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in messages.WithCancellation(cancellationToken))
            yield return message;
    }

    private static async IAsyncEnumerable<ProbeMessage> Values(params string[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();
            yield return new ProbeMessage(value);
        }
    }

    private sealed record ProbeMessage(string Value);

    private sealed class ServerStreamingBehavior(string name, List<string> order)
        : IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>
    {
        public int Order => name == "outer" ? 0 : 10;

        public async IAsyncEnumerable<ProbeMessage> InvokeAsync(
            ProbeMessage request,
            GrpcCallContext context,
            GrpcServerStreamingPipelineDelegate<ProbeMessage, ProbeMessage> next,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            order.Add($"{name}:before");
            await foreach (var item in next(request, context, cancellationToken).WithCancellation(cancellationToken))
                yield return item;
            order.Add($"{name}:after");
        }
    }

    private sealed class ClientStreamingBehavior(string name, List<string> order)
        : IGrpcClientStreamingBehavior<ProbeMessage, ProbeMessage>
    {
        public int Order => name == "outer" ? 0 : 10;

        public async Task<ProbeMessage> InvokeAsync(
            IAsyncEnumerable<ProbeMessage> requestStream,
            GrpcCallContext context,
            GrpcClientStreamingPipelineDelegate<ProbeMessage, ProbeMessage> next,
            CancellationToken cancellationToken = default)
        {
            order.Add($"{name}:before");
            var response = await next(requestStream, context, cancellationToken);
            order.Add($"{name}:after");
            return response;
        }
    }

    private sealed class DuplexStreamingBehavior(string name, List<string> order)
        : IGrpcDuplexStreamingBehavior<ProbeMessage, ProbeMessage>
    {
        public int Order => name == "outer" ? 0 : 10;

        public async IAsyncEnumerable<ProbeMessage> InvokeAsync(
            IAsyncEnumerable<ProbeMessage> requestStream,
            GrpcCallContext context,
            GrpcDuplexStreamingPipelineDelegate<ProbeMessage, ProbeMessage> next,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            order.Add($"{name}:before");
            await foreach (var item in next(requestStream, context, cancellationToken).WithCancellation(cancellationToken))
                yield return item;
            order.Add($"{name}:after");
        }
    }
}
