using Franz.Common.Caching.Abstractions;
using FluentAssertions;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Franz.Common.Caching.Tests;

public sealed class CacheOptionsTests
{
  [Fact]
  public void Default_Constructor_Should_Initialize_To_Global_Defaults()
  {
    // Act - Uses the [SetsRequiredMembers] constructor
    var options = new CacheOptions();

    // Assert
    options.DefaultAbsoluteExpiration.Should().Be(TimeSpan.FromMinutes(60));
    options.DefaultSlidingExpiration.Should().Be(TimeSpan.FromMinutes(20));
    options.KeyPrefix.Should().Be("franz:");
    options.EnableDistributedCache.Should().BeTrue();
    options.DefaultPriority.Should().Be(CacheItemPriority.Normal);
    options.DefaultEstimatedSizeInBytes.Should().Be(1024);
    options.ConnectionString.Should().BeNull();
  }

  [Fact]
  public void Should_Allow_Partial_Initialization_Via_SetsRequiredMembers()
  {
    // Act - This would fail with CS9035 if [SetsRequiredMembers] was missing
    var options = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5)
    };

    // Assert
    options.DefaultAbsoluteExpiration.Should().Be(TimeSpan.FromMinutes(5));
    // Verify other required members kept their default values
    options.DefaultSlidingExpiration.Should().Be(TimeSpan.FromMinutes(20));
  }

  [Fact]
  public void Should_Allow_Setting_ConnectionString_For_Fixtures()
  {
    // Arrange
    var conn = "localhost:6379,password=secret";

    // Act
    var options = new CacheOptions
    {
      ConnectionString = conn
    };

    // Assert
    options.ConnectionString.Should().Be(conn);
  }

  [Fact]
  public void Should_Be_Serializable_And_Deserializable()
  {
    // Arrange
    var original = new CacheOptions
    {
      DefaultAbsoluteExpiration = TimeSpan.FromMinutes(15),
      KeyPrefix = "custom:",
      DefaultPriority = CacheItemPriority.High,
      ConnectionString = "redis:6379"
    };

    // Act
    var json = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<CacheOptions>(json);

    // Assert
    deserialized.Should().NotBeNull();
    deserialized!.DefaultAbsoluteExpiration.Should().Be(original.DefaultAbsoluteExpiration);
    deserialized.KeyPrefix.Should().Be(original.KeyPrefix);
    deserialized.DefaultPriority.Should().Be(original.DefaultPriority);
    deserialized.ConnectionString.Should().Be(original.ConnectionString);
  }

  [Fact]
  public void KeyPrefix_Should_Accept_Valid_Alphanumeric_Values()
  {
    // Act
    var options = new CacheOptions { KeyPrefix = "service_v1-cache" };

    // Assert
    options.KeyPrefix.Should().Be("service_v1-cache");
  }
}