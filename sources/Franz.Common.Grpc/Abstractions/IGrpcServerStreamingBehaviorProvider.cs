namespace Franz.Common.Grpc.Abstractions;

/// <summary>Resolves server-streaming behaviors in deterministic outermost-first order.</summary>
public interface IGrpcServerStreamingBehaviorProvider
{
    IGrpcServerStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class;
}
