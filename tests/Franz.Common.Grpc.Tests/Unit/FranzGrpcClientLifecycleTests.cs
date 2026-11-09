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
        var provider = Substitute.For<IGrpcClientBehaviorProvider>();
        provider.ResolveBehaviors<ProbeRequest, ProbeReply>()
            .Returns(Array.Empty<IGrpcClientBehavior<ProbeRequest, ProbeReply>>());

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
        var provider = Substitute.For<IGrpcClientBehaviorProvider>();
        provider.ResolveBehaviors<ProbeRequest, ProbeReply>()
            .Returns(Array.Empty<IGrpcClientBehavior<ProbeRequest, ProbeReply>>());

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
            CancellationToken cancellationToken = default)
        {
            return ExecuteUnaryAsync(
                request,
                "franz.grpc.tests.v1.GrpcProbe",
                "Unary",
                (_, _, callOptions) =>
                {
                    CapturedOptions = callOptions;
                    return Task.FromResult(new ProbeReply());
                },
                cancellationToken: cancellationToken,
                deadlineUtc: deadlineUtc);
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
