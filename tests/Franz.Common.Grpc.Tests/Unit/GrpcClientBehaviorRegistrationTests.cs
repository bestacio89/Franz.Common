using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Client;
using Franz.Common.Grpc.Client.Interceptors;
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.Tests.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcClientBehaviorRegistrationTests
{
    [Fact]
    public void AddFranzGrpcClient_RegistersCompleteDefaultUnaryPipeline()
    {
        var services = new ServiceCollection();
        services.AddFranzGrpcClient(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var behaviorProvider = scope.ServiceProvider.GetRequiredService<IGrpcClientBehaviorProvider>();
        var behaviors = behaviorProvider.ResolveBehaviors<ProbeRequest, ProbeReply>();

        Assert.Collection(
            behaviors,
            behavior => Assert.IsType<ExceptionMappingClientBehavior<ProbeRequest, ProbeReply>>(behavior),
            behavior => Assert.IsType<ValidationClientBehavior<ProbeRequest, ProbeReply>>(behavior),
            behavior => Assert.IsType<TenantResolutionClientBehavior<ProbeRequest, ProbeReply>>(behavior),
            behavior => Assert.IsType<LoggingClientBehavior<ProbeRequest, ProbeReply>>(behavior),
            behavior => Assert.IsType<MetricClientBehavior<ProbeRequest, ProbeReply>>(behavior));
    }

    [Fact]
    public void AddFranzGrpcClient_DoesNotRegisterClientAuthorizationByDefault()
    {
        var services = new ServiceCollection();
        services.AddFranzGrpcClient(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var behaviors = scope.ServiceProvider
            .GetRequiredService<IGrpcClientBehaviorProvider>()
            .ResolveBehaviors<ProbeRequest, ProbeReply>();

        Assert.DoesNotContain(behaviors, behavior =>
            behavior is AuthorizationClientBehavior<ProbeRequest, ProbeReply>);
    }
}
