using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;
using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Grpc.Tests.Infrastructure.Hosting;

/// <summary>
/// Owns the in-process host lifetime and exposes the transport resources used by integration tests.
/// Host configuration is kept in <see cref="GrpcTestHostFactory"/>.
/// </summary>
internal sealed class GrpcTestHost : IAsyncDisposable
{
    private readonly IHost _host;

    internal GrpcTestHost(
        IHost host,
        GrpcPipelineProbe pipelineProbe,
        GrpcStreamingLifecycleProbe streamingLifecycleProbe)
    {
        _host = host;
        Server = host.GetTestServer();
        PipelineProbe = pipelineProbe;
        StreamingLifecycleProbe = streamingLifecycleProbe;
    }

    public TestServer Server { get; }

    public GrpcPipelineProbe PipelineProbe { get; }

    public GrpcStreamingLifecycleProbe StreamingLifecycleProbe { get; }

    public static Task<GrpcTestHost> StartAsync(bool authenticated = false)
        => GrpcTestHostFactory.StartAsync(authenticated);

    public GrpcChannel CreateChannel()
        => GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions
            {
                HttpHandler = Server.CreateHandler()
            });

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync().ConfigureAwait(false);
        _host.Dispose();
    }
}
