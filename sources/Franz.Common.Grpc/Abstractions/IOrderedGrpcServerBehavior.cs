namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Defines the execution position of a unary server behavior.
/// Lower values execute earlier and therefore wrap higher values.
/// </summary>
public interface IOrderedGrpcServerBehavior
{
    int Order { get; }
}
