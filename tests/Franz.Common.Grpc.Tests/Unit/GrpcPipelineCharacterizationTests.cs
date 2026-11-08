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
    }

    [Fact]
    public void FromException_CurrentlyExposesUnexpectedExceptionMessageInRpcStatusDetail()
    {
        const string sensitiveMessage = "database endpoint and credentials must not be exposed";
        var outcome = GrpcOutcome.FromException(new InvalidOperationException(sensitiveMessage));

        // This remains a characterization until the dedicated error-semantics checkpoint.
        Assert.Equal(sensitiveMessage, outcome.ToRpcException().Status.Detail);
    }

    [Fact]
    public void FromException_CurrentlyMapsCancellationToInternalError()
    {
        var outcome = GrpcOutcome.FromException(new OperationCanceledException());

        // This remains a characterization until the dedicated cancellation checkpoint.
        Assert.Equal(StatusCode.Internal, outcome.StatusCode);
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
