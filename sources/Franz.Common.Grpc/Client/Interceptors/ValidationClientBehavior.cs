using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client.Interceptors; // or .Server.Interceptors if you prefer

/// <summary>
/// Server-side behavior that runs the Franz validation engine on incoming requests
/// before executing the gRPC service method.
/// </summary>
/// <typeparam name="TRequest">The incoming gRPC request type.</typeparam>
/// <typeparam name="TResponse">The outgoing gRPC response type.</typeparam>
public sealed class ValidationClientBehavior<TRequest, TResponse> : IGrpcServerBehavior<TRequest, TResponse>
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
      GrpcServerPipelineDelegate<TRequest, TResponse> next,
      CancellationToken cancellationToken = default)
  {
    if (request is null)
      throw new ArgumentNullException(nameof(request));

    // Use the gRPC method as a potential rule-set identifier
    var ruleSet = context.Method;

    // Delegate actual validation to the Franz validation engine.
    // Engine is expected to throw your validation exception type if invalid.
    await _validationEngine
        .ValidateAsync(request, ruleSet, cancellationToken)
        .ConfigureAwait(false);

    // If validation passes, continue down the pipeline.
    return await next(request, context, cancellationToken).ConfigureAwait(false);
  }
}
