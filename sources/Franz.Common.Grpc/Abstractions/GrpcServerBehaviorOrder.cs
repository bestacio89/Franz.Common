namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Defines the explicit execution position of a unary server behavior.
/// Lower values execute earlier and therefore wrap higher values.
/// </summary>
public interface IOrderedGrpcServerBehavior
{
    int Order { get; }
}

/// <summary>
/// Reserved execution positions for the built-in unary server behaviors.
/// Custom behaviors may use values between these positions to declare their
/// placement relative to the standard pipeline.
/// </summary>
public static class GrpcServerBehaviorOrder
{
    public const int ExceptionMapping = int.MinValue;
    public const int Validation = -10_000;
    public const int TenantResolution = -5_000;
    public const int Authorization = 0;
    public const int Logging = 5_000;
    public const int Metrics = 10_000;
}
