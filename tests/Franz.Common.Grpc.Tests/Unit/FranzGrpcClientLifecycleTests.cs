using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Configuration;
using Franz.Common.Grpc.Tests.Contracts;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class FranzGrpcClientLifecycleTests
{
    [Fact]
    public async Task UnaryCall_AppliesConfiguredDefaultDeadlineWhenNoExplicitDeadlineExists()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost");
        var provider = CreateBehaviorProvider();
        var options = Options.Create(new FranzGrpcClientOptions
        {
            DefaultClientTimeout = TimeSpan.FromSeconds(30)
        });
        var client = new ProbeFranzGrpcClient(new GrpcProbe.GrpcProbeClient(channel), provider, options);
        var startedAt = DateTime.UtcNow;

        await client.InvokeAsync(new ProbeRequest { Value = "default-deadline" });

        var completedAt = DateTime.UtcNow;
        var deadline = Assert.IsType<DateTime>(client.CapturedOptions?.Deadline);
        Assert.InRange(deadline, startedAt.AddSeconds(29), completedAt.AddSeconds(31));
    }

    [Fact]
    public async Task UnaryCall_PreservesExplicitDeadlineOverConfiguredDefault()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost");
        var provider = CreateBehaviorProvider();
        var options = Options.Create(new FranzGrpcClientOptions
        {
            DefaultClientTimeout = TimeSpan.FromSeconds(30)
        });
        var client = new ProbeFranzGrpcClient(new GrpcProbe.GrpcProbeClient(channel), provider, options);
        var explicitDeadline = DateTime.UtcNow.AddMinutes(2);

        await client.InvokeAsync(
            new ProbeRequest { Value = "explicit-deadline" },
            deadlineUtc: explicitDeadline);

        Assert.Equal(explicitDeadline, client.CapturedOptions?.Deadline);
    }

    [Fact]
    public async Task UnaryCall_PropagatesCancellationTokenPassedThroughBehaviorPipeline()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost");
        using var originalCancellation = new CancellationTokenSource();
        using var behaviorCancellation = new CancellationTokenSource();
        var provider = Substitute.For<IGrpcClientBehaviorProvider>();
        provider.ResolveBehaviors<ProbeRequest, ProbeReply>()
            .Returns(new IGrpcClientBehavior<ProbeRequest, ProbeReply>[]
            {
                new CancellationOverrideBehavior(behaviorCancellation.Token)
            });

        var client = new ProbeFranzGrpcClient(
            new GrpcProbe.GrpcProbeClient(channel),
            provider,
            Options.Create(new FranzGrpcClientOptions()));

        await client.InvokeAsync(
            new ProbeRequest { Value = "pipeline-cancellation" },
            cancellationToken: originalCancellation.Token);

        Assert.Equal(behaviorCancellation.Token, client.CapturedOptions?.CancellationToken);
    }

    [Fact]
    public async Task UnaryCall_PropagatesStandardAndApplicationMetadata()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost");
        var client = new ProbeFranzGrpcClient(
            new GrpcProbe.GrpcProbeClient(channel),
            CreateBehaviorProvider(),
            Options.Create(new FranzGrpcClientOptions()));

        await client.InvokeAsync(
            new ProbeRequest { Value = "metadata" },
            tenantId: "tenant-42",
            userId: "user-17",
            metadataModifier: metadata => metadata.Add("x-application-context", "orders"));

        var headers = Assert.IsType<Metadata>(client.CapturedOptions?.Headers);

        Assert.Contains(headers, entry => entry.Key == GrpcCallContext.CorrelationIdHeaderName);
        Assert.Contains(headers, entry => entry.Key == GrpcCallContext.RequestIdHeaderName);
        Assert.Contains(headers, entry => entry.Key == GrpcCallContext.TenantIdHeaderName && Equals(entry.Value, "tenant-42"));
        Assert.Contains(headers, entry => entry.Key == GrpcCallContext.UserIdHeaderName && Equals(entry.Value, "user-17"));
        Assert.Contains(headers, entry => entry.Key == "x-application-context" && Equals(entry.Value, "orders"));
    }

    [Fact]
    public async Task UnaryCall_DoesNotRetryTransportFailuresImplicitly()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost");
        var client = new ProbeFranzGrpcClient(
            new GrpcProbe.GrpcProbeClient(channel),
            CreateBehaviorProvider(),
            Options.Create(new FranzGrpcClientOptions
            {
                EnableRetries = true,
                RetryCount = 3,
                RetryBaseDelay = TimeSpan.FromMilliseconds(1)
            }));
        var transportInvocationCount = 0;

        var exception = await Assert.ThrowsAsync<RpcException>(() => client.InvokeAsync(
            new ProbeRequest { Value = "non-idempotent-command" },
            (_, _, _) =>
            {
                transportInvocationCount++;
                throw new RpcException(new Status(StatusCode.Unavailable, "transient"));
            }));

        Assert.Equal(StatusCode.Unavailable, exception.StatusCode);
        Assert.Equal(1, transportInvocationCount);
    }

    private static IGrpcClientBehaviorProvider CreateBehaviorProvider()
    {
        var provider = Substitute.For<IGrpcClientBehaviorProvider>();
        provider.ResolveBehaviors<ProbeRequest, ProbeReply>()
            .Returns(Array.Empty<IGrpcClientBehavior<ProbeRequest, ProbeReply>>());
        return provider;
    }

    private sealed class ProbeFranzGrpcClient : FranzGrpcClientBase<GrpcProbe.GrpcProbeClient>
    {
        public CallOptions? CapturedOptions { get; private set; }

        public ProbeFranzGrpcClient(
            GrpcProbe.GrpcProbeClient grpcClient,
            IGrpcClientBehaviorProvider behaviorProvider,
            IOptions<FranzGrpcClientOptions> options)
            : base(grpcClient, behaviorProvider, options)
        {
        }

        public Task<ProbeReply> InvokeAsync(
            ProbeRequest request,
            DateTime? deadlineUtc = null,
            CancellationToken cancellationToken = default,
            Action<Metadata>? metadataModifier = null,
            string? tenantId = null,
            string? userId = null)
        {
            return InvokeAsync(
                request,
                (_, _, callOptions) =>
                {
                    CapturedOptions = callOptions;
                    return Task.FromResult(new ProbeReply());
                },
                deadlineUtc,
                cancellationToken,
                metadataModifier,
                tenantId,
                userId);
        }

        public Task<ProbeReply> InvokeAsync(
            ProbeRequest request,
            Func<GrpcProbe.GrpcProbeClient, ProbeRequest, CallOptions, Task<ProbeReply>> transport,
            DateTime? deadlineUtc = null,
            CancellationToken cancellationToken = default,
            Action<Metadata>? metadataModifier = null,
            string? tenantId = null,
            string? userId = null)
        {
            return ExecuteUnaryAsync(
                request,
                "franz.grpc.tests.v1.GrpcProbe",
                "Unary",
                (grpcClient, message, callOptions) =>
                {
                    CapturedOptions = callOptions;
                    return transport(grpcClient, message, callOptions);
                },
                metadataModifier,
                tenantId,
                userId,
                cancellationToken,
                deadlineUtc);
        }
    }

    private sealed class CancellationOverrideBehavior(CancellationToken cancellationToken)
        : IGrpcClientBehavior<ProbeRequest, ProbeReply>
    {
        public Task<ProbeReply> InvokeAsync(
            ProbeRequest request,
            GrpcCallContext context,
            GrpcClientPipelineDelegate<ProbeRequest, ProbeReply> next,
            CancellationToken pipelineCancellationToken = default)
        {
            return next(request, context, cancellationToken);
        }
    }
}
