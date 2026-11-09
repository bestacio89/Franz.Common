using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Standard .NET diagnostics for Franz gRPC. Subscribe to this ActivitySource and
/// Meter to export traces and metrics through OpenTelemetry or another provider.
/// Metrics contain only service, method, RPC shape, direction, and outcome tags.
/// </summary>
public static class FranzGrpcDiagnostics
{
    public const string ActivitySourceName = "Franz.Common.Grpc";
    public const string MeterName = "Franz.Common.Grpc";

    private static readonly ActivitySource Source = new(ActivitySourceName);
    private static readonly Meter DiagnosticsMeter = new(MeterName);
    private static readonly Counter<long> Calls = DiagnosticsMeter.CreateCounter<long>(
        "franz.grpc.calls",
        "{call}",
        "Completed gRPC calls.");
    private static readonly Histogram<double> Duration = DiagnosticsMeter.CreateHistogram<double>(
        "franz.grpc.call.duration",
        "s",
        "Duration of completed gRPC calls.");

    internal static Activity? StartActivity(GrpcMetricContext context)
    {
        var kind = context.Direction == "client" ? ActivityKind.Client : ActivityKind.Server;
        var activity = Source.StartActivity(
            $"grpc.{context.Direction}",
            kind);

        if (activity is null)
            return null;

        activity.SetTag("rpc.system", "grpc");
        activity.SetTag("rpc.service", context.ServiceName);
        activity.SetTag("rpc.method", context.MethodName);
        activity.SetTag("rpc.shape", context.RpcShape);
        activity.SetTag("rpc.direction", context.Direction);

        return activity;
    }

    internal static void RecordCompleted(
        GrpcMetricContext context,
        bool succeeded,
        double durationSeconds)
    {
        var tags = new TagList
        {
            { "rpc.service", context.ServiceName },
            { "rpc.method", context.MethodName },
            { "rpc.shape", context.RpcShape },
            { "rpc.direction", context.Direction },
            { "rpc.outcome", succeeded ? "success" : "failure" }
        };

        Calls.Add(1, tags);
        Duration.Record(durationSeconds, tags);
    }

    internal static void SetActivityOutcome(Activity? activity, bool succeeded, Exception? exception)
    {
        if (activity is null)
            return;

        if (succeeded)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error);
        if (exception is not null)
            activity.SetTag("error.type", exception.GetType().FullName);
    }
}
