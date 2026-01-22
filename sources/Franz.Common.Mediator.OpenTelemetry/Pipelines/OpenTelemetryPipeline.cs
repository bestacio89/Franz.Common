using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.OpenTelemetry.Core;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Franz.Common.Mediator.OpenTelemetry
{
  public class OpenTelemetryPipeline<TRequest, TResponse>
  : IPipeline<TRequest, TResponse>
  {
    private readonly IHostEnvironment _env;

    public OpenTelemetryPipeline(IHostEnvironment env)
    {
      _env = env;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
      using var activity =
        FranzActivityFactory.StartMediatorActivity<TRequest>(
          _env, MediatorContext.Current);

      try
      {
        var response = await next();
        activity?.SetStatus(ActivityStatusCode.Ok);
        return response;
      }
      catch (Exception ex)
      {
        activity?.AddException(ex);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
      }
    }
  }


}
