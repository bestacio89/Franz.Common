using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors;

/// <summary>
/// Validates an outgoing gRPC request before invoking the transport pipeline.
/// The gRPC method remains the validation rule-set identifier.
/// </summary>
public sealed class ValidationClientBehavior<TRequest, TResponse>
    : IGrpcClientBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzValidationEngine _validationEngine;

  public ValidationClientBehavior(IFranzValidationEngine validationEngine)
  {
    _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcClientPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(next);

    await _validationEngine
        .ValidateAsync(request, context.Method, cancellationToken)
        .ConfigureAwait(false);

    return await next(request, context, cancellationToken).ConfigureAwait(false);
  }
}
