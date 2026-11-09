using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>Next delegate for a client-streaming RPC.</summary>
public delegate Task<TResponse> GrpcClientStreamingPipelineDelegate<TRequest, TResponse>(
    IAsyncEnumerable<TRequest> requestStream,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;
