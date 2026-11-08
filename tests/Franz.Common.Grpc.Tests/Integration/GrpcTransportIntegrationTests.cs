using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Tests.Contracts;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Grpc.Tests.Integration;

public sealed class GrpcTransportIntegrationTests
{
    [Fact]
    public async Task UnaryCall_TraversesHttp2Transport()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.Server.CreateHandler()
        });
        var client = new GrpcProbe.GrpcProbeClient(channel);

        var response = await client.UnaryAsync(new ProbeRequest { Value = "unary" });

        Assert.Equal("unary", response.Value);
    }

    [Fact]
    public async Task ServerStreamingCall_TransfersMultipleResponses()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.Server.CreateHandler()
        });
        var client = new GrpcProbe.GrpcProbeClient(channel);

        using var call = client.ServerStream(new ProbeRequest { Value = "server" });
        var values = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
            values.Add(call.ResponseStream.Current.Value);

        Assert.Equal(new[] { "server-1", "server-2" }, values);
    }

    [Fact]
    public async Task ClientStreamingCall_AggregatesIncomingMessages()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.Server.CreateHandler()
        });
        var client = new GrpcProbe.GrpcProbeClient(channel);

        using var call = client.ClientStream();
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "first" });
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "second" });
        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseAsync;

        Assert.Equal("first,second", response.Value);
    }

    [Fact]
    public async Task DuplexCall_TransfersMessagesInBothDirections()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.Server.CreateHandler()
        });
        var client = new GrpcProbe.GrpcProbeClient(channel);

        using var call = client.Duplex();
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "one" });
        await call.RequestStream.WriteAsync(new ProbeRequest { Value = "two" });
        await call.RequestStream.CompleteAsync();

        var values = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
            values.Add(call.ResponseStream.Current.Value);

        Assert.Equal(new[] { "one-duplex", "two-duplex" }, values);
    }

    [Fact]
    public async Task UnaryCall_CharacterizesCallerSuppliedUserMetadataAsCurrentContextInput()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.Server.CreateHandler()
        });
        var client = new GrpcProbe.GrpcProbeClient(channel);
        var headers = new Metadata { { GrpcCallContext.UserIdHeaderName, "caller-controlled-user" } };

        var response = await client.UnaryAsync(
            new ProbeRequest { Value = "identity" },
            headers);

        // Characterization test: FromServerCallContext currently treats x-user-id metadata as identity.
        // Phase 3 must replace this with an authenticated-principal source and change this expectation.
        Assert.Equal("caller-controlled-user", response.ObservedUserId);
    }
}

internal sealed class GrpcTestHost : IAsyncDisposable
{
    private GrpcTestHost(TestServer server) => Server = server;

    public TestServer Server { get; }

    public static Task<GrpcTestHost> StartAsync()
    {
        var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services => services.AddGrpc())
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGrpcService<GrpcProbeService>());
            }));

        return Task.FromResult(new GrpcTestHost(server));
    }

    public ValueTask DisposeAsync()
    {
        Server.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class GrpcProbeService : GrpcProbe.GrpcProbeBase
{
    public override Task<ProbeReply> Unary(ProbeRequest request, ServerCallContext context)
    {
        var franzContext = GrpcCallContext.FromServerCallContext(context);
        return Task.FromResult(new ProbeReply
        {
            Value = request.Value,
            ObservedUserId = franzContext.UserId ?? string.Empty,
            ObservedCorrelationId = franzContext.CorrelationId
        });
    }

    public override async Task ServerStream(
        ProbeRequest request,
        IServerStreamWriter<ProbeReply> responseStream,
        ServerCallContext context)
    {
        await responseStream.WriteAsync(new ProbeReply { Value = $"{request.Value}-1" });
        await responseStream.WriteAsync(new ProbeReply { Value = $"{request.Value}-2" });
    }

    public override async Task<ProbeReply> ClientStream(
        IAsyncStreamReader<ProbeRequest> requestStream,
        ServerCallContext context)
    {
        var values = new List<string>();
        while (await requestStream.MoveNext(context.CancellationToken))
            values.Add(requestStream.Current.Value);

        return new ProbeReply { Value = string.Join(',', values) };
    }

    public override async Task Duplex(
        IAsyncStreamReader<ProbeRequest> requestStream,
        IServerStreamWriter<ProbeReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext(context.CancellationToken))
            await responseStream.WriteAsync(new ProbeReply { Value = $"{requestStream.Current.Value}-duplex" });
    }
}
