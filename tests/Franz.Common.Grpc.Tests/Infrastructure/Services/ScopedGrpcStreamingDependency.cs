using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

namespace Franz.Common.Grpc.Tests.Infrastructure.Services;

/// <summary>
/// Scoped dependency used to observe the exact disposal boundary of a streaming RPC.
/// </summary>
internal sealed class ScopedGrpcStreamingDependency(
    GrpcStreamingLifecycleProbe lifecycleProbe) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        lifecycleProbe.RecordScopeDisposal();
        return ValueTask.CompletedTask;
    }
}
