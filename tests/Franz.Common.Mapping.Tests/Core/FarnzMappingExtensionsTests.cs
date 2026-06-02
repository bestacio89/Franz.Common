using FluentAssertions;
using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using Franz.Common.Mapping.Extensions;
using Franz.Common.Mapping.Profiles;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace Franz.Common.Mapping.Tests.Extensions;

public class MappingExtensionsTests
{
  // =========================================================
  // TEST PROFILES & MODELS
  // =========================================================
  private sealed class TestSource { public string Value { get; set; } = string.Empty; }
  private sealed class TestDestination { public string Value { get; set; } = string.Empty; }

  private sealed class StubProfile : FranzMapProfile
  {
    public StubProfile()
    {
      CreateMap<TestSource, TestDestination>();
    }
  }

  // =========================================================
  // DEPENDENCY INJECTION LIFETIME & CONTAINER TESTS
  // =========================================================
  [Fact]
  public async Task AddFranzMapping_ShouldRegisterCoreServicesAsSingletons()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzMapping(typeof(MappingExtensionsTests).Assembly);

    // Fix: Utilize await using to handle the container's IAsyncDisposable validation loop smoothly
    await using var provider = services.BuildServiceProvider();

    // Assert
    // 1. Verify Configuration Resolution & Lifecycle
    var config1 = provider.GetService<MappingConfiguration>();
    var config2 = provider.GetService<MappingConfiguration>();
    config1.Should().NotBeNull();
    config1.Should().BeSameAs(config2);

    // 2. Verify Mapper Resolution & Lifecycle
    var mapper1 = provider.GetService<IFranzMapper>();
    var mapper2 = provider.GetService<IFranzMapper>();
    mapper1.Should().NotBeNull().And.BeOfType<FranzMapper>();
    mapper1.Should().BeSameAs(mapper2);

    // 3. Verify Mapping Service Resolution & Lifecycle
    var service1 = provider.GetService<IMappingService>();
    var service2 = provider.GetService<IMappingService>();
    service1.Should().NotBeNull().And.BeOfType<MappingService>();
    service1.Should().BeSameAs(service2);
  }

  // =========================================================
  // PROFILE DISCOVERY TESTS
  // =========================================================
  [Fact]
  public async Task AddFranzMapping_ShouldDiscoverAndApplyProfilesFromSpecifiedAssemblies()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzMapping(typeof(MappingExtensionsTests).Assembly);
    await using var provider = services.BuildServiceProvider();
    var config = provider.GetRequiredService<MappingConfiguration>();

    // Assert
    config.HasMapping(typeof(TestSource), typeof(TestDestination)).Should().BeTrue();
  }

  [Fact]
  public async Task AddFranzMapping_ShouldExecuteInlineConfigurationDelegate()
  {
    // Arrange
    var services = new ServiceCollection();
    var delegateExecuted = false;

    // Act
    services.AddFranzMapping(cfg =>
    {
      cfg.Register(new MappingExpression<TestSource, TestDestination>());
      delegateExecuted = true;
    }, typeof(MappingExtensionsTests).Assembly);

    await using var provider = services.BuildServiceProvider();
    var config = provider.GetRequiredService<MappingConfiguration>();

    // Assert
    delegateExecuted.Should().BeTrue();
    config.HasMapping(typeof(TestSource), typeof(TestDestination)).Should().BeTrue();
  }

  [Fact]
  public async Task AddFranzMapping_WithoutExplicitConfiguration_ShouldResolveCorrectly()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFranzMapping(typeof(MappingExtensionsTests).Assembly);
    await using var provider = services.BuildServiceProvider();

    var act = () => provider.GetRequiredService<IMappingService>();

    // Assert
    act.Should().NotThrow();
  }
}