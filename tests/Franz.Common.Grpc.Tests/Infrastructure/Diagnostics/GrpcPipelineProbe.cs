using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

/// <summary>
/// Captures identity and context observations at distinct points in the gRPC pipeline.
/// </summary>
internal sealed class GrpcPipelineProbe
{
    public const string ContextMarkerHeaderName = "x-franz-test-pipeline-context";

    private int _invocationCount;
    private string? _lastUserId;
    private string? _lastAuthorizedUserId;
    private string? _lastServiceUserId;
    private string? _lastServiceContextMarker;

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public string? LastUserId => Volatile.Read(ref _lastUserId);

    public string? LastAuthorizedUserId => Volatile.Read(ref _lastAuthorizedUserId);

    public string? LastServiceUserId => Volatile.Read(ref _lastServiceUserId);

    public string? LastServiceContextMarker => Volatile.Read(ref _lastServiceContextMarker);

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
