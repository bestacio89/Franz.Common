using FluentAssertions;
using Franz.Common.Caching.Abstractions;
using Franz.Common.Caching.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace Franz.Common.Caching.Tests.Providers;

public sealed class MemoryCacheConcurrencyTests
{
  [Fact]
  public async Task GetOrSetAsync_ConcurrentRequests_ShouldInvokeFactoryOnce()
  {
    using var memory = new MemoryCache(new MemoryCacheOptions());
    var options = new Mock<IOptionsMonitor<CacheOptions>>();
    options.SetupGet(x => x.CurrentValue).Returns(new CacheOptions());
    var provider = new MemoryCacheProvider(memory, options.Object);
    var factoryCalls = 0;

    var tasks = Enumerable.Range(0, 12).Select(_ =>
        provider.GetOrSetAsync("concurrent", async _ =>
        {
          Interlocked.Increment(ref factoryCalls);
          await Task.Delay(40);
          return "shared";
        }));

    var results = await Task.WhenAll(tasks);

    factoryCalls.Should().Be(1);
    results.Should().OnlyContain(result => result.Value == "shared");
    results.Count(result => result.IsHit).Should().Be(11);
  }

  [Fact]
  public async Task GetOrSetAsync_ShouldTreatNullAsAHit()
  {
    using var memory = new MemoryCache(new MemoryCacheOptions());
    var options = new Mock<IOptionsMonitor<CacheOptions>>();
    options.SetupGet(x => x.CurrentValue).Returns(new CacheOptions());
    var provider = new MemoryCacheProvider(memory, options.Object);

    await provider.SetAsync<string?>("nullable-entry", null);

    var factoryCalls = 0;
    var result = await provider.GetOrSetAsync<string?>("nullable-entry", _ =>
    {
      factoryCalls++;
      return Task.FromResult<string?>("factory");
    });

    result.IsHit.Should().BeTrue();
    result.Value.Should().BeNull();
    factoryCalls.Should().Be(0);
  }

  [Fact]
  public async Task SetAsync_ShouldReplaceAnExistingValue()
  {
    using var memory = new MemoryCache(new MemoryCacheOptions());
    var options = new Mock<IOptionsMonitor<CacheOptions>>();
    options.SetupGet(x => x.CurrentValue).Returns(new CacheOptions());
    var provider = new MemoryCacheProvider(memory, options.Object);

    await provider.SetAsync("settings:theme", "dark");
    var result = await provider.GetOrSetAsync("settings:theme", _ => Task.FromResult("light"));

    result.IsHit.Should().BeTrue();
    result.Value.Should().Be("dark");
  }
}
