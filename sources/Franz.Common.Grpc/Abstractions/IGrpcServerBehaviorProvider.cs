namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Resolves server behaviors in deterministic outermost-first order.
/// </summary>
public interface IGrpcServerBehaviorProvider
{
    IGrpcServerBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class;
}
