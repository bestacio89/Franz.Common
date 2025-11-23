using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Server.Interceptors;

public sealed class ValidationServerBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
  private readonly IFranzValidationEngine _validationEngine;

  public ValidationServerBehavior(IFranzValidationEngine validationEngine)
  {
    _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
  }

  public async Task<TResponse> InvokeAsync(
      TRequest request,
      GrpcCallContext context,
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    await _validationEngine.ValidateAsync(request, context.Method, cancellationToken)
        .ConfigureAwait(false);

    return await next(request, context, cancellationToken)
        .ConfigureAwait(false);
  }
}
