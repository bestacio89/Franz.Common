namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Defines the execution position of a server-streaming, client-streaming,
/// or duplex-streaming behavior. Lower values execute earlier and wrap higher values.
/// </summary>
public interface IOrderedGrpcStreamingBehavior
{
    int Order { get; }
}
