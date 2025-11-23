namespace Franz.Common.Grpc.Abstractions;

public interface IGrpcClientBehaviorProvider
{
  IGrpcClientBehavior<TRequest, TResponse>[] ResolveBehaviors<TRequest, TResponse>()
      where TRequest : class
      where TResponse : class;
}
