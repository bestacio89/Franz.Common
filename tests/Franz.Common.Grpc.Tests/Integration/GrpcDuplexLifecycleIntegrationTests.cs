using Franz.Common.Grpc.Tests.Infrastructure.Hosting;
using Franz.Common.Grpc.Tests.Contracts;

namespace Franz.Common.Grpc.Tests.Integration;

public sealed class GrpcDuplexLifecycleIntegrationTests
{
    [Fact]
    public async Task DuplexCall_KeepsScopedDependenciesAliveUntilBothStreamsComplete()
    {
        await using var host = await GrpcTestHost.StartAsync();
        host.StreamingLifecycleProbe.HoldDuplexStreaming();
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        using var call = client.Duplex();

        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "first" });

        Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
        Assert.Equal("first-duplex", call.ResponseStream.Current.Value);
        await host.StreamingLifecycleProbe.WaitForDuplexStreamingPauseAsync();

        Assert.Equal(0, host.StreamingLifecycleProbe.ScopeDisposalCount);

        host.StreamingLifecycleProbe.ReleaseDuplexStreaming();
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "second" });
        await call.RequestStream.CompleteAsync();

        Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
        Assert.Equal("second-duplex", call.ResponseStream.Current.Value);
        Assert.False(await call.ResponseStream.MoveNext(CancellationToken.None));

        await host.StreamingLifecycleProbe.WaitForScopeDisposalAsync();
        Assert.Equal(1, host.StreamingLifecycleProbe.ScopeDisposalCount);
    }

    [Fact]
    public async Task DuplexCall_CancellationDisposesScopedDependenciesDuringActiveStream()
    {
        await using var host = await GrpcTestHost.StartAsync();
        host.StreamingLifecycleProbe.HoldDuplexStreaming();
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        var call = client.Duplex();

        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "cancel" });

        Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
        Assert.Equal("cancel-duplex", call.ResponseStream.Current.Value);
        await host.StreamingLifecycleProbe.WaitForDuplexStreamingPauseAsync();

        Assert.Equal(0, host.StreamingLifecycleProbe.ScopeDisposalCount);

        call.Dispose();

        await host.StreamingLifecycleProbe.WaitForScopeDisposalAsync();
        Assert.Equal(1, host.StreamingLifecycleProbe.ScopeDisposalCount);
    }
}
