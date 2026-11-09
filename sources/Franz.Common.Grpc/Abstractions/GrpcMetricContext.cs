using System;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Bounded metric dimensions for a gRPC call. Identity, tenant, correlation,
/// peer, and request identifiers are intentionally not represented.
/// </summary>
public sealed record GrpcMetricContext(
    string ServiceName,
    string MethodName,
    string RpcShape,
    string Direction)
{
    internal static GrpcMetricContext Create(
        GrpcCallContext context,
        string rpcShape,
        string direction)
    {
        ArgumentNullException.ThrowIfNull(context);

        var method = context.Method ?? string.Empty;
        var separator = method.LastIndexOf('/');
        var methodName = separator >= 0 ? method[(separator + 1)..] : method;

        return new GrpcMetricContext(
            string.IsNullOrWhiteSpace(context.ServiceName) ? "unknown" : context.ServiceName,
            string.IsNullOrWhiteSpace(methodName) ? "unknown" : methodName,
            rpcShape,
            direction);
    }
}
