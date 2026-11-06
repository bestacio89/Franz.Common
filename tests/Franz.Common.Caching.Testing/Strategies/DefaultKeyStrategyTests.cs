using FluentAssertions;
using Franz.Common.Caching.Estrategies;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Caching.Tests.Strategies;

public sealed class DefaultCacheKeyStrategyTests
{
    private interface IRequest { }
    private sealed record TestRequest(int Id, string Name) : IRequest;

    [Fact]
    public void GetKey_Should_Include_Runtime_Type_Name()
    {
        var strategy = new DefaultCacheKeyStrategy(TestJsonSerializerOptions.Create());
        var key = strategy.GetKey(new TestRequest(1, "a"));

        key.Should().StartWith($"{typeof(TestRequest).FullName}:");
    }

    [Fact]
    public void BuildKey_Should_Be_Deterministic_ForEquivalentRequests()
    {
        var strategy = new DefaultCacheKeyStrategy(TestJsonSerializerOptions.Create());

        strategy.BuildKey(new TestRequest(1, "a"))
            .Should()
            .Be(strategy.BuildKey(new TestRequest(1, "a")));
    }

    [Fact]
    public void BuildKey_Should_Change_When_Request_Changes()
    {
        var strategy = new DefaultCacheKeyStrategy(TestJsonSerializerOptions.Create());

        var key1 = strategy.BuildKey(new TestRequest(1, "a"));
        var key2 = strategy.BuildKey(new TestRequest(2, "a"));

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void BuildKey_Should_Use_RuntimeType_When_Request_Is_Passed_Through_Interface()
    {
        var strategy = new DefaultCacheKeyStrategy(TestJsonSerializerOptions.Create());
        IRequest request = new TestRequest(7, "runtime");

        var key = strategy.BuildKey(request);

        key.Should().StartWith($"{typeof(TestRequest).FullName}:");
    }

    [Fact]
    public void BuildKey_Should_Use_SourceGeneratedMetadata()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = CacheKeyTestJsonContext.Default
        };
        var strategy = new DefaultCacheKeyStrategy(options);

        var key = strategy.BuildKey(new CacheKeySourceGeneratedRequest(17, "aot"));

        key.Should().StartWith($"{typeof(CacheKeySourceGeneratedRequest).FullName}:");
    }

    [Fact]
    public void BuildKey_Should_Not_Expose_RequestPayload()
    {
        const string sensitiveValue = "customer-secret-value";
        var strategy = new DefaultCacheKeyStrategy(TestJsonSerializerOptions.Create());

        var key = strategy.BuildKey(new TestRequest(1, sensitiveValue));

        key.Should().NotContain(sensitiveValue);
    }
}

internal sealed record CacheKeySourceGeneratedRequest(int Id, string Name);

[JsonSerializable(typeof(CacheKeySourceGeneratedRequest))]
internal partial class CacheKeyTestJsonContext : JsonSerializerContext
{
}
