using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

namespace Franz.Common.Grpc.Tests.Infrastructure.Authorization;

/// <summary>
/// Explicit permissive authorization implementation for isolated integration tests.
/// It must never be registered by production application defaults.
/// </summary>
internal sealed class AllowAllGrpcAuthorizationService(GrpcPipelineProbe probe) : IFranzAuthorizationService
{
    public Task AuthorizeAsync(
        GrpcCallContext context,
        CancellationToken cancellationToken = default)
    {
        probe.RecordAuthorization(context.UserId);
        return Task.CompletedTask;
    }
}
