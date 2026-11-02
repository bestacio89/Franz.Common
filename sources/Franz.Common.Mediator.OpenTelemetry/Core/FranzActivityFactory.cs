using Franz.Common.Mediator.Context;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;

namespace Franz.Common.Mediator.OpenTelemetry.Core;

public static class FranzActivityFactory
{
  public const string SourceName = "Franz";

  private static readonly ActivitySource Source = new(SourceName);

  /// <summary>
  /// Default source retained for callers that use the factory without dependency injection.
  /// Application pipelines should use the source registered by AddFranzTelemetry.
  /// </summary>
  public static ActivitySource DefaultSource => Source;

  public static Activity? StartMediatorActivity<T>(IHostEnvironment env)
      => StartMediatorActivity<T>(Source, env);

  public static Activity? StartMediatorActivity<T>(ActivitySource source, IHostEnvironment env)
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(env);

    var correlationId = MediatorContext.EnsureCorrelationId();
    var name = typeof(T).Name;
    var operation = InferOperation(name);
    var activity = source.StartActivity($"{operation}.{name}", ActivityKind.Internal);

    Enrich(activity, env, correlationId);
    activity?.SetTag(FranzSemanticConventions.MediatorType, operation);
    activity?.SetTag(FranzSemanticConventions.MediatorName, name);

    return activity;
  }

  public static Activity? StartEventActivity<T>(IHostEnvironment env)
      => StartEventActivity<T>(Source, env);

  public static Activity? StartEventActivity<T>(ActivitySource source, IHostEnvironment env)
  {
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(env);

    var correlationId = MediatorContext.EnsureCorrelationId();
    var name = typeof(T).Name;
    var activity = source.StartActivity($"event.{name}", ActivityKind.Consumer);

    Enrich(activity, env, correlationId);
    activity?.SetTag(FranzSemanticConventions.EventName, name);
    activity?.SetTag(FranzSemanticConventions.EventType, typeof(T).FullName ?? name);

    return activity;
  }

  private static void Enrich(Activity? activity, IHostEnvironment env, Guid correlationId)
  {
    if (activity is null)
      return;

    var context = MediatorContext.Current;

    activity.SetTag(FranzSemanticConventions.CorrelationId, correlationId);
    activity.SetTag(FranzSemanticConventions.UserId, context.UserId);
    activity.SetTag(FranzSemanticConventions.TenantId, context.TenantId);
    activity.SetTag(FranzSemanticConventions.Culture, context.Culture?.Name);
    activity.SetTag(FranzSemanticConventions.Environment, env.EnvironmentName);
  }

  private static string InferOperation(string name)
  {
    if (name.EndsWith("Command", StringComparison.Ordinal)) return "command";
    if (name.EndsWith("Query", StringComparison.Ordinal)) return "query";
    if (name.EndsWith("Event", StringComparison.Ordinal)) return "event";
    return "request";
  }
}
