using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Tests.Contracts;
using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

namespace Franz.Common.Grpc.Tests.Behaviors;

/// <summary>
/// Pass-through streaming behavior that proves each streaming RPC traverses the
/// registered Franz pipeline and that the terminal service receives its final context.
/// </summary>
internal sealed class ProbeStreamingBehavior(GrpcPipelineProbe probe) :
    IGrpcServerStreamingBehavior<ProbeRequest, ProbeReply>,
    IGrpcClientStreamingBehavior<ProbeRequest, ProbeReply>,
    IGrpcDuplexStreamingBehavior<ProbeRequest, ProbeReply>
{
    public int Order => 2_500;

    public IAsyncEnumerable<ProbeReply> InvokeAsync(
        ProbeRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<ProbeRequest, ProbeReply> next,
        CancellationToken cancellationToken = default)
    {
        probe.RecordServerStreamingBehaviorInvocation();
        return next(request, context.WithAdditionalHeader(GrpcPipelineProbe.ContextMarkerHeaderName, "pipeline-context"), cancellationToken);
    }

    public Task<ProbeReply> InvokeAsync(
        IAsyncEnumerable<ProbeRequest> requestStream,
        GrpcCallContext context,
        GrpcClientStreamingPipelineDelegate<ProbeRequest, ProbeReply> next,
        CancellationToken cancellationToken = default)
    {
        probe.RecordClientStreamingBehaviorInvocation();
        return next(requestStream, context.WithAdditionalHeader(GrpcPipelineProbe.ContextMarkerHeaderName, "pipeline-context"), cancellationToken);
    }

    public IAsyncEnumerable<ProbeReply> InvokeAsync(
        IAsyncEnumerable<ProbeRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<ProbeRequest, ProbeReply> next,
        CancellationToken cancellationToken = default)
    {
        probe.RecordDuplexStreamingBehaviorInvocation();
        return next(requestStream, context.WithAdditionalHeader(GrpcPipelineProbe.ContextMarkerHeaderName, "pipeline-context"), cancellationToken);
    }
}
