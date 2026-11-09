using System;
using System.Threading;
using Franz.Common.Grpc.Abstractions;
using Grpc.Core;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcOutcomeMappingTests
{
    [Theory]
    [InlineData(typeof(ArgumentException), StatusCode.InvalidArgument)]
    [InlineData(typeof(InvalidOperationException), StatusCode.FailedPrecondition)]
    [InlineData(typeof(UnauthorizedAccessException), StatusCode.Unauthenticated)]
    public void FromException_MapsKnownPlatformFailures(Type exceptionType, StatusCode expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "sensitive internal detail")!;

        var outcome = GrpcOutcome.FromException(exception);

        Assert.Equal(expectedStatus, outcome.StatusCode);
        Assert.DoesNotContain("sensitive internal detail", outcome.ToRpcException().Status.Detail);
    }

    [Fact]
    public void FromException_SanitizesUnexpectedExceptionDetails()
    {
        var outcome = GrpcOutcome.FromException(
            new InvalidOperationException("database endpoint and credentials must not be exposed"));

        var rpcException = outcome.ToRpcException();

        Assert.Equal(StatusCode.FailedPrecondition, rpcException.StatusCode);
        Assert.Equal("The operation cannot be completed in the current state.", rpcException.Status.Detail);
        Assert.DoesNotContain("database endpoint", rpcException.Status.Detail);
    }

    [Fact]
    public void FromException_MapsCallCancellationToCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var outcome = GrpcOutcome.FromException(
            new OperationCanceledException("private cancellation detail"),
            cancellation.Token);

        Assert.Equal(StatusCode.Cancelled, outcome.StatusCode);
        Assert.Equal("The call was cancelled.", outcome.ToRpcException().Status.Detail);
    }

    [Fact]
    public void FromException_MapsExpiredDeadlineToDeadlineExceeded()
    {
        var outcome = GrpcOutcome.FromException(
            new OperationCanceledException("private deadline detail"),
            deadlineUtc: DateTime.UtcNow.AddSeconds(-1));

        Assert.Equal(StatusCode.DeadlineExceeded, outcome.StatusCode);
        Assert.Equal("The call deadline was exceeded.", outcome.ToRpcException().Status.Detail);
    }

    [Fact]
    public void FromException_DoesNotTreatUnrelatedCancellationAsCallerCancellation()
    {
        var outcome = GrpcOutcome.FromException(
            new OperationCanceledException("private operation detail"));

        Assert.Equal(StatusCode.Internal, outcome.StatusCode);
        Assert.Equal("An unexpected server error occurred.", outcome.ToRpcException().Status.Detail);
    }

    [Fact]
    public void FromException_PreservesRpcStatusButSanitizesDetailsAndUntrustedTrailers()
    {
        var trailers = new Metadata
        {
            { "x-error-code", "UPSTREAM_FAILURE" },
            { "x-correlation-id", "upstream-correlation" },
            { "authorization", "Bearer secret-token" },
            { "x-private-detail", "internal host name" }
        };
        var exception = new RpcException(
            new Status(StatusCode.Unavailable, "database password must not be disclosed"),
            trailers);

        var rpcException = GrpcOutcome.FromException(exception).ToRpcException();

        Assert.Equal(StatusCode.Unavailable, rpcException.StatusCode);
        Assert.Equal("The requested service is temporarily unavailable.", rpcException.Status.Detail);
        Assert.Equal("UPSTREAM_FAILURE", rpcException.Trailers.GetValue("x-error-code"));
        Assert.Null(rpcException.Trailers.GetValue("authorization"));
        Assert.Null(rpcException.Trailers.GetValue("x-private-detail"));
    }

    [Fact]
    public void FromException_PropagatesOnlyTheTrustedCorrelationIdentifier()
    {
        var outcome = GrpcOutcome.FromException(
            new Exception("private detail"),
            correlationId: "trusted-correlation");

        var rpcException = outcome.ToRpcException();

        Assert.Equal("trusted-correlation", rpcException.Trailers.GetValue("x-correlation-id"));
        Assert.DoesNotContain("private detail", rpcException.Status.Detail);
    }
}
