namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Shared ordering contract for server-streaming, client-streaming, and duplex-streaming behaviors.
/// Lower values execute earlier and therefore wrap higher values.
/// </summary>
public interface IOrderedGrpcStreamingBehavior
{
    int Order { get; }
}

/// <summary>
/// Reserved positions for built-in streaming behaviors. Custom behaviors may use
/// values between these positions to declare deterministic placement.
/// </summary>
public static class GrpcStreamingBehaviorOrder
{
    public const int ExceptionMapping = int.MinValue;
    public const int Validation = -10_000;
    public const int TenantResolution = -5_000;
    public const int Authorization = 0;
    public const int Logging = 5_000;
    public const int Metrics = 10_000;
}
