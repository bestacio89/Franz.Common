using System.Collections.Generic;
using System.Threading;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>Next delegate for a server-streaming RPC.</summary>
public delegate IAsyncEnumerable<TResponse> GrpcServerStreamingPipelineDelegate<TRequest, TResponse>(
    TRequest request,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;
