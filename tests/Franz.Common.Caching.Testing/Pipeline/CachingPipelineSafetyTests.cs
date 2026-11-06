using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Options;
using Franz.Common.Caching.Pipelines;
using Franz.Common.Mediator.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Franz.Common.Caching.Tests.Pipelines;

public sealed class CachingPipelineSafetyTests
{
    private sealed record TestResponse(string Value);
    private sealed record ReadQuery(int Id) : IQuery<TestResponse>;
    private sealed record WriteCommand(int Id) : ICommand<TestResponse>;

    private static CachingPipeline<TRequest, TestResponse> CreatePipeline<TRequest>(
        ICacheProvider cache,
        ICacheKeyStrategy? keyStrategy = null)
        where TRequest : notnull
    {
        var options = new Mock<IOptionsMonitor<MediatorCachingOptions>>();
        options.SetupGet(x => x.CurrentValue).Returns(new MediatorCachingOptions());

        return new CachingPipeline<TRequest, TestResponse>(
            cache,
            options.Object,
            keyStrategy ?? Mock.Of<ICacheKeyStrategy>(x => x.BuildKey(It.IsAny<TRequest>()) == "test-key"),
            Mock.Of<ILogger<CachingPipeline<TRequest, TestResponse>>>(),
            Array.Empty<ICacheMetadataProvider>());
    }

    [Fact]
    public async Task Handle_CommandRequest_BypassesCacheEvenWhenNoMetadataProvidersAreRegistered()
    {
        var cache = new Mock<ICacheProvider>(MockBehavior.Strict);
        var pipeline = CreatePipeline<ICommand<TestResponse>>(cache.Object);
        var expected = new TestResponse("executed");
        var nextCalls = 0;

        var actual = await pipeline.Handle(
            new WriteCommand(42),
            () =>
            {
                nextCalls++;
                return Task.FromResult(expected);
            });

        Assert.Equal(expected, actual);
        Assert.Equal(1, nextCalls);
        cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_QueryRequest_UsesCacheWhenNoMetadataProvidersAreRegistered()
    {
        var cache = new Mock<ICacheProvider>(MockBehavior.Strict);
        var expected = new TestResponse("cached");
        cache.Setup(x => x.GetOrSetAsync(
                "test-key",
                It.IsAny<Func<CancellationToken, Task<TestResponse>>>(),
                It.IsAny<CacheOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CacheResult<TestResponse>(expected, IsHit: true));

        var pipeline = CreatePipeline<IQuery<TestResponse>>(cache.Object);
        var nextCalls = 0;

        var actual = await pipeline.Handle(
            new ReadQuery(42),
            () =>
            {
                nextCalls++;
                return Task.FromResult(new TestResponse("must-not-run"));
            });

        Assert.Equal(expected, actual);
        Assert.Equal(0, nextCalls);
        cache.VerifyAll();
    }
}
