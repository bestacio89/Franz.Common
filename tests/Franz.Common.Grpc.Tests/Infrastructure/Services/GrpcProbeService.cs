using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Tests.Contracts;
using Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;
using Grpc.Core;

namespace Franz.Common.Grpc.Tests.Infrastructure.Services;

/// <summary>
/// Minimal transport endpoint used to verify all four gRPC RPC shapes.
/// It contains no host setup, authentication, authorization, or test assertions.
/// </summary>
internal sealed class GrpcProbeService(GrpcPipelineProbe probe) : GrpcProbe.GrpcProbeBase
{
    public override Task<ProbeReply> Unary(ProbeRequest request, ServerCallContext context)
    {
        var franzContext = GrpcCallContext.FromServerCallContext(context);
        probe.RecordServiceContext(franzContext);

        return Task.FromResult(new ProbeReply
        {
            Value = request.Value,
            ObservedUserId = franzContext.UserId ?? string.Empty,
            ObservedCorrelationId = franzContext.CorrelationId
        });
    }

    public override async Task ServerStream(
        ProbeRequest request,
        IServerStreamWriter<ProbeReply> responseStream,
        ServerCallContext context)
    {
        await responseStream.WriteAsync(new ProbeReply { Value = $"{request.Value}-1" });
        await responseStream.WriteAsync(new ProbeReply { Value = $"{request.Value}-2" });
    }

    public override async Task<ProbeReply> ClientStream(
        IAsyncStreamReader<ProbeRequest> requestStream,
        ServerCallContext context)
    {
        var values = new List<string>();

        while (await requestStream.MoveNext(context.CancellationToken))
            values.Add(requestStream.Current.Value);

        return new ProbeReply { Value = string.Join(',', values) };
    }

    public override async Task Duplex(
        IAsyncStreamReader<ProbeRequest> requestStream,
        IServerStreamWriter<ProbeReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            await responseStream.WriteAsync(
                new ProbeReply { Value = $"{requestStream.Current.Value}-duplex" });
        }
    }
}
