using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcStreamingBehaviorProviderTests
{
    [Fact]
    public void AddFranzGrpcServer_RegistersAllThreeScopedStreamingProviders()
    {
        var services = new ServiceCollection();
        services.AddFranzGrpcServer(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<GrpcServerStreamingBehaviorProvider>(
            scope.ServiceProvider.GetRequiredService<IGrpcServerStreamingBehaviorProvider>());
        Assert.IsType<GrpcClientStreamingBehaviorProvider>(
            scope.ServiceProvider.GetRequiredService<IGrpcClientStreamingBehaviorProvider>());
        Assert.IsType<GrpcDuplexStreamingBehaviorProvider>(
            scope.ServiceProvider.GetRequiredService<IGrpcDuplexStreamingBehaviorProvider>());
    }

    [Fact]
    public void ServerStreamingProvider_SortsByOrderAndRejectsDuplicatePositions()
    {
        var services = new ServiceCollection();
        services.AddScoped<IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>>(_ => new ServerBehavior(20));
        services.AddScoped<IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>>(_ => new ServerBehavior(10));
        services.AddScoped<IGrpcServerStreamingBehaviorProvider, GrpcServerStreamingBehaviorProvider>();

        using var provider = services.BuildServiceProvider();
        var behaviorProvider = provider.GetRequiredService<IGrpcServerStreamingBehaviorProvider>();
        var ordered = behaviorProvider.ResolveBehaviors<ProbeMessage, ProbeMessage>();

        Assert.Equal(new[] { 10, 20 }, ordered.Select(behavior => behavior.Order));

        services.AddScoped<IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>>(_ => new ServerBehavior(10));
        using var duplicateProvider = services.BuildServiceProvider();
        var duplicateBehaviorProvider = duplicateProvider.GetRequiredService<IGrpcServerStreamingBehaviorProvider>();

        var exception = Assert.Throws<InvalidOperationException>(
            () => duplicateBehaviorProvider.ResolveBehaviors<ProbeMessage, ProbeMessage>());
        Assert.Contains("same execution order", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientAndDuplexStreamingProviders_ResolveTheirOwnContracts()
    {
        var services = new ServiceCollection();
        services.AddScoped<IGrpcClientStreamingBehavior<ProbeMessage, ProbeMessage>>(_ => new ClientBehavior());
        services.AddScoped<IGrpcDuplexStreamingBehavior<ProbeMessage, ProbeMessage>>(_ => new DuplexBehavior());
        services.AddScoped<IGrpcClientStreamingBehaviorProvider, GrpcClientStreamingBehaviorProvider>();
        services.AddScoped<IGrpcDuplexStreamingBehaviorProvider, GrpcDuplexStreamingBehaviorProvider>();

        using var provider = services.BuildServiceProvider();
        Assert.Single(provider.GetRequiredService<IGrpcClientStreamingBehaviorProvider>()
            .ResolveBehaviors<ProbeMessage, ProbeMessage>());
        Assert.Single(provider.GetRequiredService<IGrpcDuplexStreamingBehaviorProvider>()
            .ResolveBehaviors<ProbeMessage, ProbeMessage>());
    }

    private sealed record ProbeMessage;

    private sealed class ServerBehavior(int order)
        : IGrpcServerStreamingBehavior<ProbeMessage, ProbeMessage>
    {
        public int Order => order;

        public async IAsyncEnumerable<ProbeMessage> InvokeAsync(
            ProbeMessage request,
            GrpcCallContext context,
            GrpcServerStreamingPipelineDelegate<ProbeMessage, ProbeMessage> next,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var response in next(request, context, cancellationToken).WithCancellation(cancellationToken))
                yield return response;
        }
    }

    private sealed class ClientBehavior : IGrpcClientStreamingBehavior<ProbeMessage, ProbeMessage>
    {
        public int Order => 0;

        public Task<ProbeMessage> InvokeAsync(
            IAsyncEnumerable<ProbeMessage> requestStream,
            GrpcCallContext context,
            GrpcClientStreamingPipelineDelegate<ProbeMessage, ProbeMessage> next,
            CancellationToken cancellationToken = default)
            => next(requestStream, context, cancellationToken);
    }

    private sealed class DuplexBehavior : IGrpcDuplexStreamingBehavior<ProbeMessage, ProbeMessage>
    {
        public int Order => 0;

        public IAsyncEnumerable<ProbeMessage> InvokeAsync(
            IAsyncEnumerable<ProbeMessage> requestStream,
            GrpcCallContext context,
            GrpcDuplexStreamingPipelineDelegate<ProbeMessage, ProbeMessage> next,
            CancellationToken cancellationToken = default)
            => next(requestStream, context, cancellationToken);
    }
}
