using Franz.Common.Mediator.OpenTelemetry.Core;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.OpenTelemetry;

public class OpenTelemetryPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
  private readonly ActivitySource _activitySource;
  private readonly IHostEnvironment _env;

  public OpenTelemetryPipeline(IHostEnvironment env)
      : this(FranzActivityFactory.DefaultSource, env)
  {
  }

  public OpenTelemetryPipeline(ActivitySource activitySource, IHostEnvironment env)
  {
    _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    _env = env ?? throw new ArgumentNullException(nameof(env));
  }

  public async Task<TResponse> Handle(
      TRequest request,
      Func<Task<TResponse>> next,
      CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    using var activity = FranzActivityFactory.StartMediatorActivity<TRequest>(_activitySource, _env);

    try
    {
      var response = await next().ConfigureAwait(false);
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
