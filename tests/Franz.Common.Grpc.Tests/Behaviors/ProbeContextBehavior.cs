using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Tests.Contracts;
using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

namespace Franz.Common.Grpc.Tests.Behaviors;

/// <summary>
/// Test behavior that records the trusted call identity and exposes it in the unary response.
/// It also adds a harmless marker to verify that the terminal service receives the final pipeline context.
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

        var pipelineContext = context.WithAdditionalHeader(
            GrpcPipelineProbe.ContextMarkerHeaderName,
            "pipeline-context");

        var response = await next(request, pipelineContext, cancellationToken).ConfigureAwait(false);
        response.ObservedUserId = pipelineContext.UserId ?? string.Empty;

        return response;
    }
}
