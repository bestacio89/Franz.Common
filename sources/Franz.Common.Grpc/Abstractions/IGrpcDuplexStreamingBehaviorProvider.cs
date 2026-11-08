namespace Franz.Common.Grpc.Abstractions;

/// <summary>Resolves duplex-streaming behaviors in deterministic outermost-first order.</summary>
public interface IGrpcDuplexStreamingBehaviorProvider
{
    IGrpcDuplexStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class;
}
