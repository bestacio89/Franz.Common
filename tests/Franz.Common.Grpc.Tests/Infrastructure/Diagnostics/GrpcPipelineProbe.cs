using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

/// <summary>
/// Captures identity and context observations at distinct points in the gRPC pipeline.
/// </summary>
internal sealed class GrpcPipelineProbe : IFranzTenantResolver
{
    public const string ContextMarkerHeaderName = "x-franz-test-pipeline-context";

    private int _invocationCount;
    private string? _lastUserId;
    private string? _lastAuthorizedUserId;
    private string? _lastServiceUserId;
    private string? _lastServiceContextMarker;
    private string? _lastRequestedTenantId;
    private string? _lastRequestedTenantHeader;

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public string? LastUserId => Volatile.Read(ref _lastUserId);

    public string? LastAuthorizedUserId => Volatile.Read(ref _lastAuthorizedUserId);

    public string? LastServiceUserId => Volatile.Read(ref _lastServiceUserId);

    public string? LastServiceContextMarker => Volatile.Read(ref _lastServiceContextMarker);

    public string? LastRequestedTenantId => Volatile.Read(ref _lastRequestedTenantId);

    public string? LastRequestedTenantHeader => Volatile.Read(ref _lastRequestedTenantHeader);

    public Task<string?> ResolveTenantAsync(
        GrpcCallContext context,
        CancellationToken cancellationToken = default)
    {
        Volatile.Write(ref _lastRequestedTenantId, context.TenantId);
        context.RequestHeaders.TryGetValue(GrpcCallContext.TenantIdHeaderName, out var selector);
        Volatile.Write(ref _lastRequestedTenantHeader, selector);
        return Task.FromResult<string?>(null);
    }

    public void Record(string? userId)
    {
        Volatile.Write(ref _lastUserId, userId);
        Interlocked.Increment(ref _invocationCount);
    }

    public void RecordAuthorization(string? userId)
        => Volatile.Write(ref _lastAuthorizedUserId, userId);

    public void RecordServiceContext(GrpcCallContext context)
    {
        Volatile.Write(ref _lastServiceUserId, context.UserId);
        context.RequestHeaders.TryGetValue(ContextMarkerHeaderName, out var marker);
        Volatile.Write(ref _lastServiceContextMarker, marker);
    }
}
