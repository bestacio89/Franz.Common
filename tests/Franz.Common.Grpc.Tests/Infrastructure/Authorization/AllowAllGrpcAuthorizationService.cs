using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Tests.Infrastructure.Authorization;

/// <summary>
/// Explicit permissive authorization implementation for isolated integration tests.
/// It must never be registered by production application defaults.
/// </summary>
internal sealed class AllowAllGrpcAuthorizationService : IFranzAuthorizationService
{
    public Task AuthorizeAsync(
        GrpcCallContext context,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
