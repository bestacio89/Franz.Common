using System.Collections.Generic;
using System.Threading;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>Next delegate for a bidirectional streaming RPC.</summary>
public delegate IAsyncEnumerable<TResponse> GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse>(
    IAsyncEnumerable<TRequest> requestStream,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;
