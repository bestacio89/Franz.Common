namespace Franz.Common.Grpc.Abstractions;

/// <summary>Resolves client behaviors in deterministic outermost-first order.</summary>
public interface IGrpcClientBehaviorProvider
{
    IGrpcClientBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class;
}
