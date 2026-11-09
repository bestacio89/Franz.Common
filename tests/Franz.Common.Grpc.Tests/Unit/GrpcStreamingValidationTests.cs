using System.Collections.Generic;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Server.Interceptors;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcStreamingValidationTests
{
    [Fact]
    public async Task ServerStreaming_ValidatesSingleRequestBeforeInvokingHandler()
    {
        var validation = new RecordingValidationEngine();
        var behavior = new ValidationStreamingBehavior<ProbeRequest, ProbeResponse>(validation);
        var context = CreateContext();
        var request = new ProbeRequest();
        var handlerInvoked = false;

        var stream = ((IGrpcServerStreamingBehavior<ProbeRequest, ProbeResponse>)behavior)
            .InvokeAsync(
                request,
                context,
                (_, _, _) =>
                {
                    handlerInvoked = true;
                    return GetResponses();
                });

        Assert.False(handlerInvoked);

        await foreach (var _ in stream) { }

        Assert.True(handlerInvoked);
        Assert.Collection(validation.Validated, item => Assert.Same(request, item));
    }

    [Fact]
    public async Task ClientStreaming_ValidatesEachRequestAsItIsConsumed()
    {
        var validation = new RecordingValidationEngine();
        var behavior = new ValidationStreamingBehavior<ProbeRequest, ProbeResponse>(validation);
        var context = CreateContext();
        var received = new List<ProbeRequest>();

        var response = await ((IGrpcClientStreamingBehavior<ProbeRequest, ProbeResponse>)behavior)
            .InvokeAsync(
                GetRequests(),
                context,
                async (requests, _, _) =>
                {
                    await foreach (var request in requests)
                        received.Add(request);

                    return new ProbeResponse();
                });

        Assert.NotNull(response);
        Assert.Equal(2, received.Count);
        Assert.Equal(received, validation.Validated);
    }

    [Fact]
    public async Task DuplexStreaming_ValidatesEachRequestBeforeTheHandlerConsumesIt()
    {
        var validation = new RecordingValidationEngine();
        var behavior = new ValidationStreamingBehavior<ProbeRequest, ProbeResponse>(validation);
        var context = CreateContext();
        var received = new List<ProbeRequest>();

        var stream = ((IGrpcDuplexStreamingBehavior<ProbeRequest, ProbeResponse>)behavior)
            .InvokeAsync(
                GetRequests(),
                context,
                (requests, _, _) => ConsumeRequestsAndRespondAsync(requests, received));

        await foreach (var _ in stream) { }

        Assert.Equal(2, received.Count);
        Assert.Equal(received, validation.Validated);
    }

    [Fact]
    public async Task ClientStreaming_DoesNotPassInvalidRequestToHandler()
    {
        var validation = new RecordingValidationEngine(shouldFail: true);
        var behavior = new ValidationStreamingBehavior<ProbeRequest, ProbeResponse>(validation);
        var handlerInvoked = false;
        var received = new List<ProbeRequest>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((IGrpcClientStreamingBehavior<ProbeRequest, ProbeResponse>)behavior)
                .InvokeAsync(
                    GetRequests(),
                    CreateContext(),
                    async (requests, _, _) =>
                    {
                        handlerInvoked = true;
                        await foreach (var request in requests)
                            received.Add(request);
                        return new ProbeResponse();
                    }));

        Assert.True(handlerInvoked);
        Assert.Empty(received);
    }

    private static GrpcCallContext CreateContext()
        => GrpcCallContext.CreateClient("tests.Service", "Stream") with { IsServerSide = true };

    private static async IAsyncEnumerable<ProbeRequest> GetRequests()
    {
        await Task.Yield();
        yield return new ProbeRequest();
        await Task.Yield();
        yield return new ProbeRequest();
    }

    private static async IAsyncEnumerable<ProbeResponse> GetResponses()
    {
        await Task.Yield();
        yield return new ProbeResponse();
    }

    private static async IAsyncEnumerable<ProbeResponse> ConsumeRequestsAndRespondAsync(
        IAsyncEnumerable<ProbeRequest> requests,
        ICollection<ProbeRequest> received)
    {
        await foreach (var request in requests)
            received.Add(request);

        yield return new ProbeResponse();
    }

    private sealed class RecordingValidationEngine(bool shouldFail = false) : IFranzValidationEngine
    {
        public List<object> Validated { get; } = [];

        public Task ValidateAsync<T>(
            T instance,
            string? ruleSet = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (shouldFail)
                throw new InvalidOperationException("Validation failed.");

            Validated.Add(instance);
            return Task.CompletedTask;
        }
    }

    private sealed record ProbeRequest;

    private sealed record ProbeResponse;
}
