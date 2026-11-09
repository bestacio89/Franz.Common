using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server.Interceptors;
using Franz.Common.Grpc.Tests.Contracts;
using Grpc.Core;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcStreamingExceptionMappingTests
{
    [Fact]
    public async Task ServerStreaming_MapsEnumerationFailuresAndPreservesCorrelation()
    {
        var behavior = new ExceptionMappingStreamingBehavior<ProbeRequest, ProbeReply>();
        var context = CreateContext("server-stream-correlation");
        var pipeline = ((IGrpcServerStreamingBehavior<ProbeRequest, ProbeReply>)behavior).InvokeAsync(
            new ProbeRequest(),
            context,
            (_, _, _) => FaultingResponses(),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var _ in pipeline) { }
        });

        AssertSanitized(exception, "server-stream-correlation");
    }

    [Fact]
    public async Task ClientStreaming_MapsExecutionFailuresAndPreservesCorrelation()
    {
        var behavior = new ExceptionMappingStreamingBehavior<ProbeRequest, ProbeReply>();
        var context = CreateContext("client-stream-correlation");

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            ((IGrpcClientStreamingBehavior<ProbeRequest, ProbeReply>)behavior).InvokeAsync(
                EmptyRequests(),
                context,
                (_, _, _) => throw new InvalidOperationException("private database endpoint"),
                CancellationToken.None));

        AssertSanitized(exception, "client-stream-correlation");
    }

    [Fact]
    public async Task DuplexStreaming_MapsEnumerationFailuresAndPreservesCorrelation()
    {
        var behavior = new ExceptionMappingStreamingBehavior<ProbeRequest, ProbeReply>();
        var context = CreateContext("duplex-stream-correlation");
        var pipeline = ((IGrpcDuplexStreamingBehavior<ProbeRequest, ProbeReply>)behavior).InvokeAsync(
            EmptyRequests(),
            context,
            (_, _, _) => FaultingResponses(),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var _ in pipeline) { }
        });

        AssertSanitized(exception, "duplex-stream-correlation");
    }

    [Fact]
    public void Behavior_UsesReservedOutermostStreamingOrder()
    {
        var behavior = new ExceptionMappingStreamingBehavior<ProbeRequest, ProbeReply>();

        Assert.Equal(GrpcStreamingBehaviorOrder.ExceptionMapping, behavior.Order);
    }

    private static GrpcCallContext CreateContext(string correlationId)
        => GrpcCallContext.CreateClient("tests.Service", "Stream", correlationId: correlationId);

    private static async IAsyncEnumerable<ProbeReply> FaultingResponses(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new ProbeReply();
        throw new InvalidOperationException("private database endpoint");
    }

    private static async IAsyncEnumerable<ProbeRequest> EmptyRequests()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static void AssertSanitized(RpcException exception, string correlationId)
    {
        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
        Assert.Equal(
            "The operation cannot be completed in the current state.",
            exception.Status.Detail);
        Assert.DoesNotContain("private database endpoint", exception.Status.Detail);
        Assert.Equal(correlationId, exception.Trailers.GetValue("x-correlation-id"));
    }
}
