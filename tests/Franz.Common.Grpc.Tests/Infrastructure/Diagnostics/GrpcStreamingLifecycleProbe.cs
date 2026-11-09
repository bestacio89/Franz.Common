namespace Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

/// <summary>
/// Provides deterministic gates for verifying streaming call and scoped-service lifetimes.
/// </summary>
internal sealed class GrpcStreamingLifecycleProbe
{
    private readonly TaskCompletionSource _serverStreamingPaused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseServerStreaming = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _clientStreamingPaused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseClientStreaming = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _scopeDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _holdServerStreaming;
    private int _holdClientStreaming;
    private int _scopeDisposalCount;

    public int ScopeDisposalCount => Volatile.Read(ref _scopeDisposalCount);

    public void HoldServerStreaming() => Volatile.Write(ref _holdServerStreaming, 1);

    public void HoldClientStreaming() => Volatile.Write(ref _holdClientStreaming, 1);

    public async Task PauseServerStreamingIfRequestedAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _holdServerStreaming) == 0)
            return;

        _serverStreamingPaused.TrySetResult();
        await _releaseServerStreaming.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task PauseClientStreamingIfRequestedAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _holdClientStreaming) == 0)
            return;

        _clientStreamingPaused.TrySetResult();
        await _releaseClientStreaming.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task WaitForServerStreamingPauseAsync()
        => _serverStreamingPaused.Task.WaitAsync(TimeSpan.FromSeconds(10));

    public Task WaitForClientStreamingPauseAsync()
        => _clientStreamingPaused.Task.WaitAsync(TimeSpan.FromSeconds(10));

    public void ReleaseServerStreaming() => _releaseServerStreaming.TrySetResult();

    public void ReleaseClientStreaming() => _releaseClientStreaming.TrySetResult();

    public void RecordScopeDisposal()
    {
        if (Interlocked.Increment(ref _scopeDisposalCount) == 1)
            _scopeDisposed.TrySetResult();
    }

    public Task WaitForScopeDisposalAsync()
        => _scopeDisposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
}
