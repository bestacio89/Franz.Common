namespace Franz.Common.Grpc.Abstractions;

/// <summary>Resolves server-streaming behaviors in deterministic outermost-first order.</summary>
public interface IGrpcServerStreamingBehaviorProvider
{
    IGrpcServerStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class;
}

/// <summary>Resolves client-streaming behaviors in deterministic outermost-first order.</summary>
public interface IGrpcClientStreamingBehaviorProvider
{
    IGrpcClientStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class;
}

/// <summary>Resolves duplex-streaming behaviors in deterministic outermost-first order.</summary>
public interface IGrpcDuplexStreamingBehaviorProvider
{
    IGrpcDuplexStreamingBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class;
}
