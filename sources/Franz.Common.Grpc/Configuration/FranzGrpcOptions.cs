namespace Franz.Common.Grpc.Configuration;

/// <summary>
/// Global configuration for the Franz gRPC server pipeline.
/// </summary>
public sealed record FranzGrpcOptions
{
    /// <summary>
    /// Requires every incoming gRPC call to resolve a tenant and authorize the
    /// authenticated principal's membership in that tenant.
    /// Defaults to false for services that do not require tenant isolation.
    /// </summary>
    public bool RequireTenant { get; init; }
}
