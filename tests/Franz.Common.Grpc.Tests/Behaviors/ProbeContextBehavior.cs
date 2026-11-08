using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Tests.Contracts;
using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

namespace Franz.Common.Grpc.Tests.Behaviors;

/// <summary>
/// Test behavior that records the trusted call identity and exposes it in the unary response.
/// </summary>
internal sealed class ProbeContextBehavior(GrpcPipelineProbe probe)
    : IGrpcServerBehavior<ProbeRequest, ProbeReply>
{
    public int Order => 2_500;

    public async Task<ProbeReply> InvokeAsync(
        ProbeRequest request,
        GrpcCallContext context,
        GrpcServerPipelineDelegate<ProbeRequest, ProbeReply> next,
        CancellationToken cancellationToken = default)
    {
        probe.Record(context.UserId);

        var response = await next(request, context, cancellationToken).ConfigureAwait(false);
        response.ObservedUserId = context.UserId ?? string.Empty;

        return response;
    }
}
