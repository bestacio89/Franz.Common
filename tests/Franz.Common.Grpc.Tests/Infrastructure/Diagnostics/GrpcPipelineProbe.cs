namespace Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

/// <summary>
/// Captures the last identity observed by the test pipeline without coupling assertions
/// to the behavior implementation or transport service.
/// </summary>
internal sealed class GrpcPipelineProbe
{
    private int _invocationCount;
    private string? _lastUserId;

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public string? LastUserId => Volatile.Read(ref _lastUserId);

    public void Record(string? userId)
    {
        Volatile.Write(ref _lastUserId, userId);
        Interlocked.Increment(ref _invocationCount);
    }
}
