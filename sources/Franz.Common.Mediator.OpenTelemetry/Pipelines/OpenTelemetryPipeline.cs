using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Franz.Common.Mediator.OpenTelemetry
{
  public class OpenTelemetryPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
    
  {
    private readonly ActivitySource _activitySource;
    private readonly IHostEnvironment _env; // environment awareness

    public OpenTelemetryPipeline(ActivitySource activitySource, IHostEnvironment env)
    {
      _activitySource = activitySource;
      _env = env;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
      using var activity = _activitySource.StartActivity(
          $"Mediator {typeof(TRequest).Name}",
          ActivityKind.Internal);

      if (activity != null)
      {
        var ctx = MediatorContext.Current;

        activity.SetTag("franz.correlation_id", ctx.CorrelationId);
        activity.SetTag("franz.user_id", ctx.UserId);
        activity.SetTag("franz.tenant_id", ctx.TenantId);
        activity.SetTag("franz.culture", ctx.Culture.Name);

        foreach (var kvp in ctx.Metadata)
          activity.SetTag($"franz.metadata.{kvp.Key}", kvp.Value);

        // add environment via host
        activity.SetTag("franz.environment", _env.EnvironmentName);
      }

      try
      {
        var response = await next();
        activity?.SetStatus(ActivityStatusCode.Ok);
        return response;
      }
      catch (Exception ex)
      {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag("exception.type", ex.GetType().Name);
        activity?.SetTag("exception.message", ex.Message);
        throw;
      }
    }
  }

}
