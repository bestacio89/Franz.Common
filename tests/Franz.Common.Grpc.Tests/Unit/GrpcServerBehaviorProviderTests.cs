using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcServerBehaviorProviderTests
{
    [Fact]
    public void ResolveBehaviors_SortsByExplicitOrderRatherThanRegistrationOrder()
    {
        var provider = new GrpcServerBehaviorProvider(
            new BehaviorServiceProvider<ProbeRequest, ProbeResponse>(
            [
                new LaterBehavior(),
                new EarlierBehavior()
            ]));

        var behaviors = provider.ResolveBehaviors<ProbeRequest, ProbeResponse>();

        Assert.Collection(
            behaviors,
            behavior => Assert.IsType<EarlierBehavior>(behavior),
            behavior => Assert.IsType<LaterBehavior>(behavior));
    }

    [Fact]
    public void ResolveBehaviors_RejectsAmbiguousExecutionOrders()
    {
        var provider = new GrpcServerBehaviorProvider(
            new BehaviorServiceProvider<ProbeRequest, ProbeResponse>(
            [
                new EarlierBehavior(),
                new DuplicateOrderBehavior()
            ]));

        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.ResolveBehaviors<ProbeRequest, ProbeResponse>());

        Assert.Contains("same execution order", exception.Message, StringComparison.Ordinal);
    }

    private sealed class ProbeRequest { }
    private sealed class ProbeResponse { }

    private sealed class EarlierBehavior : IGrpcServerBehavior<ProbeRequest, ProbeResponse>
    {
        public int Order => 10;

        public Task<ProbeResponse> InvokeAsync(
            ProbeRequest request,
            GrpcCallContext context,
            GrpcServerPipelineDelegate<ProbeRequest, ProbeResponse> next,
            CancellationToken cancellationToken = default)
            => next(request, context, cancellationToken);
    }

    private sealed class LaterBehavior : IGrpcServerBehavior<ProbeRequest, ProbeResponse>
    {
        public int Order => 20;

        public Task<ProbeResponse> InvokeAsync(
            ProbeRequest request,
            GrpcCallContext context,
            GrpcServerPipelineDelegate<ProbeRequest, ProbeResponse> next,
            CancellationToken cancellationToken = default)
            => next(request, context, cancellationToken);
    }

    private sealed class DuplicateOrderBehavior : IGrpcServerBehavior<ProbeRequest, ProbeResponse>
    {
        public int Order => 10;

        public Task<ProbeResponse> InvokeAsync(
            ProbeRequest request,
            GrpcCallContext context,
            GrpcServerPipelineDelegate<ProbeRequest, ProbeResponse> next,
            CancellationToken cancellationToken = default)
            => next(request, context, cancellationToken);
    }

    private sealed class BehaviorServiceProvider<TRequest, TResponse>(
        IEnumerable<IGrpcServerBehavior<TRequest, TResponse>> behaviors) : IServiceProvider
        where TRequest : class
        where TResponse : class
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(IEnumerable<IGrpcServerBehavior<TRequest, TResponse>>)
                ? behaviors
                : null;
    }
}
