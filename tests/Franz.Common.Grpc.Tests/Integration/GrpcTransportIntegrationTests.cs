using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Tests.Infrastructure.Hosting;
using Franz.Common.Grpc.Tests.Contracts;
using Grpc.Core;
using Grpc.Net.Client;

namespace Franz.Common.Grpc.Tests.Integration;

public sealed class GrpcTransportIntegrationTests
{
    [Fact]
    public async Task UnaryCall_TraversesHttp2TransportAndFranzPipeline()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        var response = await client.UnaryAsync(new ProbeRequest { Value = "unary" });
        Assert.Equal("unary", response.Value);
        Assert.Equal(string.Empty, response.ObservedUserId);
    }

    [Fact]
    public async Task UnaryCall_UsesAuthenticatedPrincipalClaimForTrustedIdentity()
    {
        await using var host = await GrpcTestHost.StartAsync(authenticated: true);
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        var response = await client.UnaryAsync(new ProbeRequest { Value = "trusted-identity" });
        Assert.Equal("trusted-user", response.ObservedUserId);
        Assert.Equal(1, host.PipelineProbe.InvocationCount);
        Assert.Equal("trusted-user", host.PipelineProbe.LastUserId);
        Assert.Equal("trusted-user", host.PipelineProbe.LastAuthorizedUserId);
        Assert.Equal("trusted-user", host.PipelineProbe.LastServiceUserId);
        Assert.Equal("pipeline-context", host.PipelineProbe.LastServiceContextMarker);
    }

    [Fact]
    public async Task ServerStreamingCall_TraversesFranzPipelineAndPreservesFinalContext()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        using var call = client.ServerStream(new ProbeRequest { Value = "server" });
        var values = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
            values.Add(call.ResponseStream.Current.Value);
        Assert.Equal(new[] { "server-1", "server-2" }, values);
        Assert.Equal(1, host.PipelineProbe.ServerStreamingBehaviorInvocationCount);
        Assert.Equal("pipeline-context", host.PipelineProbe.LastServiceContextMarker);
    }

    [Fact]
    public async Task ClientStreamingCall_TraversesFranzPipelineAndPreservesFinalContext()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        using var call = client.ClientStream();
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "first" });
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "second" });
        await call.RequestStream.CompleteAsync();
        var response = await call.ResponseAsync;
        Assert.Equal("first,second", response.Value);
        Assert.Equal(1, host.PipelineProbe.ClientStreamingBehaviorInvocationCount);
        Assert.Equal("pipeline-context", host.PipelineProbe.LastServiceContextMarker);
    }

    [Fact]
    public async Task DuplexCall_TraversesFranzPipelineAndPreservesFinalContext()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        using var call = client.Duplex();
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "one" });
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "two" });
        await call.RequestStream.CompleteAsync();
        var values = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
            values.Add(call.ResponseStream.Current.Value);
        Assert.Equal(new[] { "one-duplex", "two-duplex" }, values);
        Assert.Equal(1, host.PipelineProbe.DuplexStreamingBehaviorInvocationCount);
        Assert.Equal("pipeline-context", host.PipelineProbe.LastServiceContextMarker);
    }

    [Fact]
    public async Task UnaryCall_PassesTenantMetadataToResolverOnlyAsAnUntrustedSelector()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        const string requestedTenantId = "requested-tenant";
        var headers = new Metadata { { GrpcCallContext.TenantIdHeaderName, requestedTenantId } };
        await client.UnaryAsync(new ProbeRequest { Value = "tenant-selector" }, headers);
        Assert.Equal(requestedTenantId, host.PipelineProbe.LastRequestedTenantId);
        Assert.Equal(requestedTenantId, host.PipelineProbe.LastRequestedTenantHeader);
    }

    [Fact]
    public async Task UnaryCall_DoesNotTreatCallerSuppliedUserMetadataAsAuthenticatedIdentity()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = host.CreateChannel();
        var client = new GrpcProbe.GrpcProbeClient(channel);
        var headers = new Metadata { { GrpcCallContext.UserIdHeaderName, "caller-controlled-user" } };
        var response = await client.UnaryAsync(new ProbeRequest { Value = "identity" }, headers);
        Assert.Equal(string.Empty, response.ObservedUserId);
        Assert.Null(host.PipelineProbe.LastUserId);
        Assert.Null(host.PipelineProbe.LastAuthorizedUserId);
        Assert.Null(host.PipelineProbe.LastServiceUserId);
    }
}
