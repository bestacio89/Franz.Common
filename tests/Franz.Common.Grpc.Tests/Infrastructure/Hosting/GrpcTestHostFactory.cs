using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.AspNetCore.DependencyInjection;
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.Tests.Behaviors;
using Franz.Common.Grpc.Tests.Contracts;
using Franz.Common.Grpc.Tests.Infrastructure.Authentication;
using Franz.Common.Grpc.Tests.Infrastructure.Authorization;
using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;
using Franz.Common.Grpc.Tests.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Grpc.Tests.Infrastructure.Hosting;

/// <summary>
/// Defines the complete ASP.NET Core pipeline and service registrations for gRPC integration tests.
/// Keep middleware and service registration explicit so the test host behaves like a real application host.
/// </summary>
internal static class GrpcTestHostFactory
{
    public static async Task<GrpcTestHost> StartAsync(bool authenticated)
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
            .StartAsync()
            .ConfigureAwait(false);

        return new GrpcTestHost(host, pipelineProbe);
    }
}
