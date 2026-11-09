namespace Franz.Common.Grpc.Abstractions;

/// <summary>Resolves client-streaming behaviors in deterministic outermost-first order.</summary>
public interface IGrpcClientStreamingBehaviorProvider
{
    IGrpcClientStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class;
}
