using Franz.Common.Grpc.Tests.Contracts;
using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;
using Franz.Common.Grpc.Tests.Infrastructure.Services;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Grpc.Tests.Integration;

public sealed class GrpcClientFactoryLifecycleIntegrationTests
{
    [Fact]
    public async Task GrpcClientFactory_ReusesHostManagedHandlerAcrossClients()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(new GrpcPipelineProbe());
                    services.AddSingleton(new GrpcStreamingLifecycleProbe());
                    services.AddGrpc();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<GrpcProbeService>());
                }))
            .StartAsync();

        var server = host.GetTestServer();
        var handlerCreationCount = 0;
        var handlerDisposalCount = 0;
        var services = new ServiceCollection();

        services
            .AddGrpcClient<GrpcProbe.GrpcProbeClient>(options =>
                options.Address = new Uri("http://localhost"))
            .ConfigurePrimaryHttpMessageHandler(_ =>
            {
                Interlocked.Increment(ref handlerCreationCount);
                return new TrackingHttpMessageHandler(
                    server.CreateHandler(),
                    () => Interlocked.Increment(ref handlerDisposalCount));
            });

        await using (var provider = services.BuildServiceProvider())
        {
            var clientFactory = provider.GetRequiredService<GrpcClientFactory>();
            var clientName = nameof(GrpcProbe.GrpcProbeClient);
            var firstClient = clientFactory.CreateClient<GrpcProbe.GrpcProbeClient>(clientName);
            var secondClient = clientFactory.CreateClient<GrpcProbe.GrpcProbeClient>(clientName);

            var firstResponse = await firstClient.UnaryAsync(new ProbeRequest { Value = "first" });
            var secondResponse = await secondClient.UnaryAsync(new ProbeRequest { Value = "second" });

            Assert.Equal("first", firstResponse.Value);
            Assert.Equal("second", secondResponse.Value);
            Assert.Equal(1, Volatile.Read(ref handlerCreationCount));

            // IHttpClientFactory owns pooled handlers. Disposing this service provider does
            // not synchronously dispose an active handler; its configured handler lifetime
            // and the factory's cleanup process govern eventual disposal.
            Assert.Equal(0, Volatile.Read(ref handlerDisposalCount));
        }
    }

    private sealed class TrackingHttpMessageHandler(
        HttpMessageHandler innerHandler,
        Action onDispose) : DelegatingHandler(innerHandler)
    {
        private int _disposed;

        protected override void Dispose(bool disposing)
        {
            if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
                onDispose();

            base.Dispose(disposing);
        }
    }
}
