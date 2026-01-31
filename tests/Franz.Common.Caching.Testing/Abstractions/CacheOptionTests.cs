using Franz.Common.Caching.Abstractions;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Caching.Tests;

public sealed class CacheOptionsTests
{
  [Fact]
  public void Default_constructor_should_initialize_all_properties_to_null()
  {
    // Act
    var options = new CacheOptions();

    // Assert
    options.Expiration.Should().BeNull();
    options.LocalCacheHint.Should().BeNull();
    options.Tags.Should().BeNull();
  }

  [Fact]
  public void Should_allow_setting_expiration()
  {
    // Arrange
    var ttl = TimeSpan.FromMinutes(10);

    // Act
    var options = new CacheOptions
    {
      Expiration = ttl
    };

    // Assert
    options.Expiration.Should().Be(ttl);
  }

  [Fact]
  public void Should_allow_setting_local_cache_hint()
  {
    // Arrange
    var hint = TimeSpan.FromSeconds(30);

    // Act
    var options = new CacheOptions
    {
      LocalCacheHint = hint
    };

    // Assert
    options.LocalCacheHint.Should().Be(hint);
  }

  [Fact]
  public void Should_allow_setting_tags()
  {
    // Arrange
    var tags = new[] { "users", "by-id", "read-model" };

    // Act
    var options = new CacheOptions
    {
      Tags = tags
    };

    // Assert
    options.Tags.Should().BeEquivalentTo(tags);
  }

  [Fact]
  public void Should_allow_partial_configuration()
  {
    // Act
    var options = new CacheOptions
    {
      Expiration = TimeSpan.FromMinutes(5)
    };

    // Assert
    options.Expiration.Should().Be(TimeSpan.FromMinutes(5));
    options.LocalCacheHint.Should().BeNull();
    options.Tags.Should().BeNull();
  }

  [Fact]
  public void Should_preserve_reference_for_tags_array()
  {
    // Arrange
    var tags = new[] { "products", "electronics" };

    // Act
    var options = new CacheOptions
    {
      Tags = tags
    };

    // Assert
    ReferenceEquals(tags, options.Tags).Should().BeTrue(
        "CacheOptions should not clone or mutate tag references"
    );
  }

  [Fact]
  public void Should_be_serializable_and_deserializable()
  {
    // Arrange
    var original = new CacheOptions
    {
      Expiration = TimeSpan.FromMinutes(15),
      LocalCacheHint = TimeSpan.FromMinutes(2),
      Tags = new[] { "orders", "hot-path" }
    };

    // Act
    var json = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<CacheOptions>(json);

    // Assert
    deserialized.Should().NotBeNull();
    deserialized!.Expiration.Should().Be(original.Expiration);
    deserialized.LocalCacheHint.Should().Be(original.LocalCacheHint);
    deserialized.Tags.Should().BeEquivalentTo(original.Tags);
  }

  [Fact]
  public void Should_support_empty_tags_array()
  {
    // Act
    var options = new CacheOptions
    {
      Tags = Array.Empty<string>()
    };

    // Assert
    options.Tags.Should().NotBeNull();
    options.Tags.Should().BeEmpty();
  }
}
