using System.Collections.Generic;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcStreamingObservabilityTests
{
    [Fact]
    public async Task LoggingBehavior_RecordsNormalStreamCompletion()
    {
        var logger = Substitute.For<IFranzGrpcLogger>();
        var behavior = new LoggingStreamingBehavior<ProbeMessage, ProbeMessage>(logger);
        var context = CreateServerContext();

        var stream = ((IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>)behavior)
            .InvokeAsync(new ProbeMessage(), context, (_, _, _) => GetResponses());

        await foreach (var _ in stream) { }

        logger.Received(1).LogStreamStarted(context, "server_streaming");
        logger.Received(1).LogStreamCompleted(
            context,
            "server_streaming",
            Arg.Any<long>());
        logger.DidNotReceive().LogStreamAborted(
            Arg.Any<GrpcCallContext>(),
            Arg.Any<string>(),
            Arg.Any<long>());
    }

    [Fact]
    public async Task LoggingBehavior_RecordsEarlyResponseStreamDisposalAsAborted()
    {
        var logger = Substitute.For<IFranzGrpcLogger>();
        var behavior = new LoggingStreamingBehavior<ProbeMessage, ProbeMessage>(logger);
        var context = CreateServerContext();

        var stream = ((IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>)behavior)
            .InvokeAsync(new ProbeMessage(), context, (_, _, _) => GetResponses());

        await using var enumerator = stream.GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        await enumerator.DisposeAsync();

        logger.Received(1).LogStreamAborted(
            context,
            "server_streaming",
            Arg.Any<long>());
        logger.DidNotReceive().LogStreamCompleted(
            Arg.Any<GrpcCallContext>(),
            Arg.Any<string>(),
            Arg.Any<long>());
    }

    [Fact]
    public async Task MetricBehavior_UsesBoundedDimensionsAndRecordsNormalCompletion()
    {
        var metrics = Substitute.For<IFranzGrpcMetrics>();
        var behavior = new MetricStreamingBehavior<ProbeMessage, ProbeMessage>(metrics);
        var context = CreateServerContext() with
        {
            TenantId = "tenant-secret",
            UserId = "user-secret"
        };

        var stream = ((IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>)behavior)
            .InvokeAsync(new ProbeMessage(), context, (_, _, _) => GetResponses());

        await foreach (var _ in stream) { }

        metrics.Received(1).RecordRequest(Arg.Is<GrpcMetricContext>(metric =>
            metric.ServiceName == "tests.Service" &&
            metric.MethodName == "Stream" &&
            metric.RpcShape == "server_streaming" &&
            metric.Direction == "server"));
        metrics.Received(1).RecordSuccess(
            Arg.Is<GrpcMetricContext>(metric => metric.RpcShape == "server_streaming"),
            Arg.Any<long>());
        metrics.DidNotReceive().RecordFailure(
            Arg.Any<GrpcMetricContext>(),
            Arg.Any<Exception>(),
            Arg.Any<long>());
    }

    [Fact]
    public async Task MetricBehavior_RecordsEarlyResponseStreamDisposalAsFailure()
    {
        var metrics = Substitute.For<IFranzGrpcMetrics>();
        var behavior = new MetricStreamingBehavior<ProbeMessage, ProbeMessage>(metrics);
        var context = CreateServerContext();

        var stream = ((IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>)behavior)
            .InvokeAsync(new ProbeMessage(), context, (_, _, _) => GetResponses());

        await using var enumerator = stream.GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        await enumerator.DisposeAsync();

        metrics.Received(1).RecordFailure(
            Arg.Is<GrpcMetricContext>(metric => metric.RpcShape == "server_streaming"),
            Arg.Is<Exception>(exception => exception is OperationCanceledException),
            Arg.Any<long>());
    }

    [Fact]
    public void AddFranzGrpcServer_RegistersObservabilityForEveryStreamingShape()
    {
        var services = new ServiceCollection();
        services.AddFranzGrpcServer(new ConfigurationBuilder().Build());

        AssertBehaviorRegistration<IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>, ValidationStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
        AssertBehaviorRegistration<IGrpcClientStreamingBehavior<ProbeMessage, ProbeMessage>, ValidationStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
        AssertBehaviorRegistration<IGrpcDuplexStreamingBehavior<ProbeMessage, ProbeMessage>, ValidationStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
        AssertBehaviorRegistration<IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>, LoggingStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
        AssertBehaviorRegistration<IGrpcClientStreamingBehavior<ProbeMessage, ProbeMessage>, LoggingStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
        AssertBehaviorRegistration<IGrpcDuplexStreamingBehavior<ProbeMessage, ProbeMessage>, LoggingStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
        AssertBehaviorRegistration<IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>, MetricStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
        AssertBehaviorRegistration<IGrpcClientStreamingBehavior<ProbeMessage, ProbeMessage>, MetricStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
        AssertBehaviorRegistration<IGrpcDuplexStreamingBehavior<ProbeMessage, ProbeMessage>, MetricStreamingBehavior<ProbeMessage, ProbeMessage>>(services);
    }

    private static void AssertBehaviorRegistration<TContract, TBehavior>(IServiceCollection services)
        where TBehavior : TContract
    {
        var contractDefinition = typeof(TContract).GetGenericTypeDefinition();
        var behaviorDefinition = typeof(TBehavior).GetGenericTypeDefinition();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == contractDefinition &&
            descriptor.ImplementationType == behaviorDefinition);
    }

    private static GrpcCallContext CreateServerContext()
        => GrpcCallContext.CreateClient("tests.Service", "Stream") with { IsServerSide = true };

    private static async IAsyncEnumerable<ProbeMessage> GetResponses()
    {
        await Task.Yield();
        yield return new ProbeMessage();
        await Task.Yield();
        yield return new ProbeMessage();
    }

    private sealed record ProbeMessage;
}
