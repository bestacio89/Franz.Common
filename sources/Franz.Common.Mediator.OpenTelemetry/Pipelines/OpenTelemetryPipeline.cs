using Franz.Common.Mediator.OpenTelemetry.Core;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.OpenTelemetry;

public class OpenTelemetryPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
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
    // Removed second parameter; StartMediatorActivity reads ambient MediatorContext.Current internally
    using var activity = FranzActivityFactory.StartMediatorActivity<TRequest>(_env);

    try
    {
      var response = await next().ConfigureAwait(false);
      activity?.SetStatus(ActivityStatusCode.Ok);
      return response;
    }
    catch (Exception ex)
    {
      activity?.RecordException(ex);
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      throw;
    }
  }
}