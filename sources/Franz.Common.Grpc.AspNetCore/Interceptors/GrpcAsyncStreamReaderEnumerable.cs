using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Grpc.Core;

namespace Franz.Common.Grpc.AspNetCore.Interceptors;

/// <summary>
/// Exposes the transport request reader as a single-consumer asynchronous enumerable.
/// </summary>
internal sealed class GrpcAsyncStreamReaderEnumerable<TMessage>(IAsyncStreamReader<TMessage> reader)
    : IAsyncEnumerable<TMessage>
{
    private int _enumerationStarted;

    public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _enumerationStarted, 1) != 0)
        {
            throw new InvalidOperationException(
                "A gRPC request stream can only be enumerated once.");
        }

        return ReadAllAsync(cancellationToken).GetAsyncEnumerator();
    }

    private async IAsyncEnumerable<TMessage> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await reader.MoveNext(cancellationToken).ConfigureAwait(false))
        {
            yield return reader.Current;
        }
    }
}
