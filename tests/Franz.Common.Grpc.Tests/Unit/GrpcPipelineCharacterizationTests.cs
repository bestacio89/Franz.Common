using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server.Interceptors;
using Franz.Common.Grpc.Server.Pipelines;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcPipelineCharacterizationTests
{
    [Fact]
    public async Task ExceptionMappingBehavior_DoesNotMapExceptionsThrownByEarlierBehaviors()
    {
        var behaviors = new IGrpcServerBehavior<ProbeRequest, ProbeResponse>[]
        {
            new ThrowingBehavior(),
            new ExceptionMappingServerBehavior<ProbeRequest, ProbeResponse>()
        };

        var pipeline = FranzServerInteropPipeline.Build(
            behaviors,
            (_, _, _) => Task.FromResult(new ProbeResponse()));

        // Characterizes the current pipeline order: exception mapping is innermost,
        // so an exception from an earlier/outer behavior bypasses the mapper.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline(new ProbeRequest(), GrpcCallContext.CreateClient("tests.Service", "Call"), CancellationToken.None));
    }

    [Fact]
    public void FromException_CurrentlyExposesUnexpectedExceptionMessageInRpcStatusDetail()
    {
        const string sensitiveMessage = "database endpoint and credentials must not be exposed";
        var outcome = GrpcOutcome.FromException(new InvalidOperationException(sensitiveMessage));

        // Characterization only. The production contract must sanitize unexpected failures.
        Assert.Equal(sensitiveMessage, outcome.ToRpcException().Status.Detail);
    }

    [Fact]
    public void FromException_CurrentlyMapsCancellationToInternalError()
    {
        var outcome = GrpcOutcome.FromException(new OperationCanceledException());

        // Characterization only. Cancellation must receive deliberate transport semantics.
        Assert.Equal(Grpc.Core.StatusCode.Internal, outcome.StatusCode);
    }

    private sealed class ProbeRequest;
    private sealed class ProbeResponse;

    private sealed class ThrowingBehavior : IGrpcServerBehavior<ProbeRequest, ProbeResponse>
    {
        public Task<ProbeResponse> InvokeAsync(
            ProbeRequest request,
            GrpcCallContext context,
            GrpcServerPipelineDelegate<ProbeRequest, ProbeResponse> next,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("outer behavior failure");
    }
}
