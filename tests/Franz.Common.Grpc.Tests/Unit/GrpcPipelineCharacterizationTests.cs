using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server.Interceptors;
using Franz.Common.Grpc.Server.Pipelines;
using Grpc.Core;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcPipelineCharacterizationTests
{
    [Fact]
    public async Task ExceptionMappingBehavior_MapsExceptionsThrownByEveryPipelineBehavior()
    {
        var behaviors = new IGrpcServerBehavior<ProbeRequest, ProbeResponse>[]
        {
            new ExceptionMappingServerBehavior<ProbeRequest, ProbeResponse>(),
            new ThrowingBehavior()
        };

        var pipeline = FranzServerInteropPipeline.Build(
            behaviors,
            (_, _, _) => Task.FromResult(new ProbeResponse()));

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            pipeline(new ProbeRequest(), GrpcCallContext.CreateClient("tests.Service", "Call"), CancellationToken.None));

        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal("An unexpected server error occurred.", exception.Status.Detail);
    }

    [Fact]
    public async Task ExceptionMappingBehavior_PropagatesTrustedCorrelationIdentifier()
    {
        var behavior = new ExceptionMappingServerBehavior<ProbeRequest, ProbeResponse>();
        var context = GrpcCallContext.CreateClient(
            "tests.Service",
            "Call",
            correlationId: "trusted-correlation");

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            behavior.InvokeAsync(
                new ProbeRequest(),
                context,
                (_, _, _) => throw new InvalidOperationException("private failure detail")));

        Assert.Equal("trusted-correlation", exception.Trailers.GetValue("x-correlation-id"));
        Assert.DoesNotContain("private failure detail", exception.Status.Detail);
    }

    [Fact]
    public void FromException_DoesNotExposeUnexpectedExceptionMessageInRpcStatusDetail()
    {
        const string sensitiveMessage = "database endpoint and credentials must not be exposed";
        var outcome = GrpcOutcome.FromException(new InvalidOperationException(sensitiveMessage));

        Assert.Equal(StatusCode.FailedPrecondition, outcome.StatusCode);
        Assert.Equal("The operation cannot be completed in the current state.", outcome.ToRpcException().Status.Detail);
        Assert.DoesNotContain(sensitiveMessage, outcome.ToRpcException().Status.Detail);
    }

    [Fact]
    public void FromException_ClassifiesUnrelatedCancellationAsInternalError()
    {
        var outcome = GrpcOutcome.FromException(new OperationCanceledException());

        Assert.Equal(StatusCode.Internal, outcome.StatusCode);
        Assert.Equal("An unexpected server error occurred.", outcome.ToRpcException().Status.Detail);
    }

    private sealed class ProbeRequest { }
    private sealed class ProbeResponse { }

    private sealed class ProbeFailureException : Exception
    {
        public ProbeFailureException(string message) : base(message) { }
    }

    private sealed class ThrowingBehavior : IGrpcServerBehavior<ProbeRequest, ProbeResponse>
    {
        public int Order => 100_000;

        public Task<ProbeResponse> InvokeAsync(
            ProbeRequest request,
            GrpcCallContext context,
            GrpcServerPipelineDelegate<ProbeRequest, ProbeResponse> next,
            CancellationToken cancellationToken = default)
            => throw new ProbeFailureException("outer behavior failure");
    }
}
