using System.Collections.Generic;
using Grpc.Core;

namespace Franz.Common.Grpc.AspNetCore.Interceptors;

/// <summary>
/// Adapts a pipeline-provided asynchronous enumerable to the reader contract
/// expected by generated ASP.NET Core gRPC service methods.
/// </summary>
internal sealed class GrpcAsyncEnumerableStreamReader<TMessage>(
    IAsyncEnumerable<TMessage> source,
    CancellationToken pipelineCancellationToken) : IAsyncStreamReader<TMessage>, IAsyncDisposable
    where TMessage : class
{
    private IAsyncEnumerator<TMessage>? _enumerator;
    private CancellationTokenSource? _linkedCancellation;
    private TMessage? _current;
    private int _moveNextInProgress;
    private bool _completed;

    public TMessage Current => _current
        ?? throw new InvalidOperationException("The request stream has no current message.");

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _moveNextInProgress, 1) != 0)
        {
            throw new InvalidOperationException(
                "Concurrent MoveNext calls are not supported for a gRPC request stream.");
        }

        try
        {
            if (_completed)
            {
                return false;
            }

            if (_enumerator is null)
            {
                _linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    pipelineCancellationToken,
                    cancellationToken);
                _enumerator = source.GetAsyncEnumerator(_linkedCancellation.Token);
            }

            try
            {
                if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    _current = _enumerator.Current;
                    return true;
                }

                _completed = true;
                _current = null;
                await DisposeEnumeratorAsync().ConfigureAwait(false);
                return false;
            }
            catch
            {
                _completed = true;
                await DisposeEnumeratorAsync().ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            Volatile.Write(ref _moveNextInProgress, 0);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _completed = true;
        _current = null;
        await DisposeEnumeratorAsync().ConfigureAwait(false);
    }

    private async ValueTask DisposeEnumeratorAsync()
    {
        if (_enumerator is not null)
        {
            await _enumerator.DisposeAsync().ConfigureAwait(false);
            _enumerator = null;
        }

        _linkedCancellation?.Dispose();
        _linkedCancellation = null;
    }
}
