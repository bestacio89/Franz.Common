using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>Represents the next behavior or service method in the server pipeline.</summary>
public delegate Task<TResponse> GrpcServerPipelineDelegate<TRequest, TResponse>(
    TRequest request,
    GrpcCallContext context,
    CancellationToken cancellationToken)
    where TRequest : class
    where TResponse : class;
