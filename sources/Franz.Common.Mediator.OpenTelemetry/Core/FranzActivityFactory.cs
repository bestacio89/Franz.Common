using Franz.Common.Mediator.Context;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Franz.Common.Mediator.OpenTelemetry.Core;


public static class FranzActivityFactory
{
  public const string SourceName = "Franz";

  private static readonly ActivitySource Source = new(SourceName);

  public static Activity? StartMediatorActivity<T>(
      IHostEnvironment env,
      MediatorContext ctx)
  {
    var name = typeof(T).Name;
    var operation = InferOperation(name);

    var activity = Source.StartActivity(
        $"{operation}.{name}",
        ActivityKind.Internal);

    Enrich(activity, env, ctx);

    activity?.SetTag(FranzSemanticConventions.MediatorType, operation);
    activity?.SetTag(FranzSemanticConventions.MediatorName, name);

    return activity;
  }

  public static Activity? StartEventActivity<T>(
      IHostEnvironment env,
      MediatorContext ctx)
  {
    var name = typeof(T).Name;

    var activity = Source.StartActivity(
        $"event.{name}",
        ActivityKind.Consumer);

    Enrich(activity, env, ctx);

    activity?.SetTag(FranzSemanticConventions.EventName, name);
    activity?.SetTag(FranzSemanticConventions.EventType, typeof(T).FullName ?? name);

    return activity;
  }

  private static void Enrich(
      Activity? activity,
      IHostEnvironment env,
      MediatorContext ctx)
  {
    if (activity == null || ctx == null) return;

    activity.SetTag(FranzSemanticConventions.CorrelationId, ctx.CorrelationId);
    activity.SetTag(FranzSemanticConventions.UserId, ctx.UserId);
    activity.SetTag(FranzSemanticConventions.TenantId, ctx.TenantId);
    activity.SetTag(FranzSemanticConventions.Culture, ctx.Culture?.Name);
    activity.SetTag(FranzSemanticConventions.Environment, env.EnvironmentName);

    foreach (var kvp in ctx.Metadata)
      activity.SetTag($"franz.metadata.{kvp.Key}", kvp.Value);
  }

  private static string InferOperation(string name)
  {
    if (name.EndsWith("Command")) return "command";
    if (name.EndsWith("Query")) return "query";
    if (name.EndsWith("Event")) return "event";
    return "request";
  }
}
