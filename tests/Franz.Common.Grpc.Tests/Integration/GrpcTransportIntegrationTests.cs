using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.AspNetCore.DependencyInjection;
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.Tests.Contracts;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Franz.Common.Grpc.Tests.Integration;

public sealed class GrpcTransportIntegrationTests
{
    [Fact]
    public async Task UnaryCall_TraversesHttp2TransportAndFranzPipeline()
    {
        await using var host = await GrpcTestHost.StartAsync();
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.Server.CreateHandler()
        });
        var client = new GrpcProbe.GrpcProbeClient(channel);

        var response = await client.UnaryAsync(new ProbeRequest { Value = "unary" });

        Assert.Equal("unary", response.Value);
        Assert.Equal(string.Empty, response.ObservedUserId);
    }

    [Fact]
    public async Task UnaryCall_UsesAuthenticatedPrincipalClaimForTrustedIdentity()
    {
        await using var host = await GrpcTestHost.StartAsync(authenticated: true);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.Server.CreateHandler()
        });
        var client = new GrpcProbe.GrpcProbeClient(channel);

        var response = await client.UnaryAsync(new ProbeRequest { Value = "trusted-identity" });

        Assert.Equal("trusted-user", response.ObservedUserId);
        Assert.Equal(1, host.PipelineProbe.InvocationCount);
        Assert.Equal("trusted-user", host.PipelineProbe.LastUserId);
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
    public async Task UnaryCall_DoesNotTreatCallerSuppliedUserMetadataAsAuthenticatedIdentity()
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

        Assert.Equal(string.Empty, response.ObservedUserId);
    }
}

internal sealed class GrpcTestHost : IAsyncDisposable
{
    private readonly IHost _host;

    private GrpcTestHost(IHost host, GrpcPipelineProbe pipelineProbe)
    {
        _host = host;
        Server = host.GetTestServer();
        PipelineProbe = pipelineProbe;
    }

    public TestServer Server { get; }
    public GrpcPipelineProbe PipelineProbe { get; }

    public static async Task<GrpcTestHost> StartAsync(bool authenticated = false)
    {
        var pipelineProbe = new GrpcPipelineProbe();
        var host = await new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddSingleton(pipelineProbe);
                    services.AddFranzGrpcServer(new ConfigurationBuilder().Build());
                    services.AddFranzGrpcDefaults();
                    services.AddSingleton<IFranzAuthorizationService, AllowAllGrpcAuthorizationService>();
                    services.AddScoped<IGrpcServerBehavior<ProbeRequest, ProbeReply>, ProbeContextBehavior>();
                    services.AddFranzGrpcAspNetCore();

                    services
                        .AddAuthentication(TestAuthenticationHandler.SchemeName)
                        .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.SchemeName,
                            options => options.Authenticate = authenticated);
                    services.AddAuthorization();
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<GrpcProbeService>());
                });
            })
            .StartAsync();

        return new GrpcTestHost(host, pipelineProbe);
    }

    public ValueTask DisposeAsync()
    {
        _host.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    public bool Authenticate { get; set; }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "FranzGrpcIntegrationTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.Authenticate)
            return Task.FromResult(AuthenticateResult.NoResult());

        var principal = new ClaimsPrincipal(new[]
        {
            new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "untrusted-user")
            }),
            new ClaimsIdentity(authenticationType: "AuthenticatedWithoutSubject"),
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "trusted-user") },
                authenticationType: SchemeName)
        });

        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class AllowAllGrpcAuthorizationService : IFranzAuthorizationService
{
    public Task AuthorizeAsync(GrpcCallContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal sealed class GrpcPipelineProbe
{
    private int _invocationCount;
    private string? _lastUserId;

    public int InvocationCount => Volatile.Read(ref _invocationCount);
    public string? LastUserId => Volatile.Read(ref _lastUserId);

    public void Record(string? userId)
    {
        Volatile.Write(ref _lastUserId, userId);
        Interlocked.Increment(ref _invocationCount);
    }
}

internal sealed class ProbeContextBehavior(GrpcPipelineProbe probe) : IGrpcServerBehavior<ProbeRequest, ProbeReply>
{
    public int Order => 2_500;

    public async Task<ProbeReply> InvokeAsync(
        ProbeRequest request,
        GrpcCallContext context,
        GrpcServerPipelineDelegate<ProbeRequest, ProbeReply> next,
        CancellationToken cancellationToken = default)
    {
        probe.Record(context.UserId);
        var response = await next(request, context, cancellationToken).ConfigureAwait(false);
        response.ObservedUserId = context.UserId ?? string.Empty;
        return response;
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
