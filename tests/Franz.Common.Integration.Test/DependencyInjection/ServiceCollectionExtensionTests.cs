using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using System;

namespace Franz.Common.Integration.Tests.DependencyInjection
{
  // Dummy marker types for scanning tests
  public interface IFakeService { }
  public interface IOtherService { }

  public sealed class FakeScopedService : IFakeService, IScopedDependency { }
  public sealed class FakeSingletonService : IOtherService, ISingletonDependency { }

  public class ServiceCollectionExtensionsTests
  {
    [Fact]
    public void AddDependencies_Should_Register_Scoped_And_Singleton_Services()
    {
      var services = new ServiceCollection();
      services.AddDependencies(asm => asm == typeof(FakeScopedService).Assembly);

      using var provider = services.BuildServiceProvider();

      var scoped = provider.GetRequiredService<IFakeService>();
      var single = provider.GetRequiredService<IOtherService>();

      scoped.Should().BeOfType<FakeScopedService>();
      single.Should().BeOfType<FakeSingletonService>();
    }

    [Fact]
    public void AddNoDuplicateSingleton_Should_Not_Add_Duplicates()
    {
      var services = new ServiceCollection();
      services.AddSingleton<IFakeService, FakeScopedService>();
      services.AddNoDuplicateSingleton<IFakeService, FakeScopedService>();

      services.Count(s => s.ServiceType == typeof(IFakeService))
              .Should().Be(1);
    }

    [Fact]
    public void AddNoDuplicateTransient_Should_Not_Add_Duplicates()
    {
      var services = new ServiceCollection();
      services.AddTransient<IFakeService, FakeScopedService>();
      services.AddNoDuplicateTransient<IFakeService, FakeScopedService>();

      services.Count(s => s.ServiceType == typeof(IFakeService))
              .Should().Be(1);
    }

    [Fact]
    public void AddNoDuplicateScoped_Should_Not_Add_Duplicates()
    {
      var services = new ServiceCollection();
      services.AddScoped<IFakeService, FakeScopedService>();
      services.AddNoDuplicateScoped<IFakeService, FakeScopedService>();

      services.Count(s => s.ServiceType == typeof(IFakeService))
              .Should().Be(1);
    }

    [Fact]
    public void AddSelfScoped_Should_Register_Self_Type_As_Scoped()
    {
      var services = new ServiceCollection();
      services.AddSelfScoped<FakeScopedService>(asm => asm == typeof(FakeScopedService).Assembly);

      using var provider = services.BuildServiceProvider();
      var instance = provider.GetRequiredService<FakeScopedService>();

      instance.Should().NotBeNull();
    }

    [Fact]
    public void AddMatchingInterfaceScoped_Should_Register_With_Matching_Interface()
    {
      var services = new ServiceCollection();
      services.AddMatchingInterfaceScoped<IFakeService>(asm => asm == typeof(FakeScopedService).Assembly);

      using var provider = services.BuildServiceProvider();
      var resolved = provider.GetRequiredService<IFakeService>();

      resolved.Should().BeOfType<FakeScopedService>();
    }

    [Fact]
    public void AddImplementedInterfaceSingleton_Should_Register_Singleton_Service()
    {
      var services = new ServiceCollection();
      services.AddImplementedInterfaceSingleton<ISingletonDependency>(asm => asm == typeof(FakeSingletonService).Assembly);

      using var provider = services.BuildServiceProvider();
      var service = provider.GetRequiredService<ISingletonDependency>();

      service.Should().BeOfType<FakeSingletonService>();
    }

    [Fact]
    public void AddImplementedInterfaceTransient_Should_Register_Transient_Service()
    {
      var services = new ServiceCollection();
      services.AddImplementedInterfaceTransient<IScopedDependency>(asm => asm == typeof(FakeScopedService).Assembly);

      using var provider = services.BuildServiceProvider();
      var s1 = provider.GetRequiredService<IScopedDependency>();
      var s2 = provider.GetRequiredService<IScopedDependency>();

      s1.Should().NotBeSameAs(s2); // transient = new instance each time
    }
  }
}
