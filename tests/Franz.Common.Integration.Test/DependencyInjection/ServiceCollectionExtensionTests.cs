using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using System;

namespace Franz.Common.Integration.Tests.DependencyInjection
{
  // Dummy marker types for scanning tests
  public interface IFakeService : IScopedDependency { }
  public interface IOtherService { }

  public sealed class FakeScopedService : IFakeService, IScopedDependency, IDisposable
  {
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
  }

  public sealed class FakeSingletonService : IOtherService, ISingletonDependency { }

  public class ServiceCollectionExtensionsTests
  {
    [Fact]
    public void AddDependencies_Should_Register_Marker_Lifetimes_And_Respect_Scope_Boundaries()
    {
      var services = new ServiceCollection();
      services.AddDependencies(asm => asm == typeof(FakeScopedService).Assembly);

      var scopedRegistration = services.Single(service => service.ServiceType == typeof(IFakeService));
      var singletonRegistration = services.Single(service => service.ServiceType == typeof(IOtherService));

      scopedRegistration.Lifetime.Should().Be(ServiceLifetime.Scoped);
      singletonRegistration.Lifetime.Should().Be(ServiceLifetime.Singleton);
      services.Should().NotContain(service =>
        service.ServiceType == typeof(IScopedDependency)
        || service.ServiceType == typeof(ISingletonDependency));

      using var provider = services.BuildServiceProvider(new ServiceProviderOptions
      {
        ValidateScopes = true,
        ValidateOnBuild = true
      });

      var firstScope = provider.CreateScope();
      var first = firstScope.ServiceProvider.GetRequiredService<IFakeService>();
      var firstAgain = firstScope.ServiceProvider.GetRequiredService<IFakeService>();
      var singletonFromFirstScope = firstScope.ServiceProvider.GetRequiredService<IOtherService>();

      first.Should().BeSameAs(firstAgain);

      firstScope.Dispose();
      ((FakeScopedService)first).IsDisposed.Should().BeTrue();

      using var secondScope = provider.CreateScope();
      var second = secondScope.ServiceProvider.GetRequiredService<IFakeService>();
      var singletonFromSecondScope = secondScope.ServiceProvider.GetRequiredService<IOtherService>();

      second.Should().NotBeSameAs(first);
      singletonFromSecondScope.Should().BeSameAs(singletonFromFirstScope);
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

      using var provider = services.BuildServiceProvider(new ServiceProviderOptions
      {
        ValidateScopes = true,
        ValidateOnBuild = true
      });
      var instance = provider.GetRequiredService<FakeScopedService>();

      instance.Should().NotBeNull();
    }

    [Fact]
    public void AddImplementedInterfaceSingleton_Should_Register_Singleton_Service()
    {
      var services = new ServiceCollection();
      services.AddImplementedInterfaceSingleton<ISingletonDependency>(asm => asm == typeof(FakeSingletonService).Assembly);

      using var provider = services.BuildServiceProvider(new ServiceProviderOptions
      {
        ValidateScopes = true,
        ValidateOnBuild = true
      });
      var service = provider.GetRequiredService<ISingletonDependency>();

      service.Should().BeOfType<FakeSingletonService>();
    }

    [Fact]
    public void AddImplementedInterfaceTransient_Should_Register_Transient_Service()
    {
      var services = new ServiceCollection();
      services.AddImplementedInterfaceTransient<IScopedDependency>(asm => asm == typeof(FakeScopedService).Assembly);

      using var provider = services.BuildServiceProvider(new ServiceProviderOptions
      {
        ValidateScopes = true,
        ValidateOnBuild = true
      });
      using var scope = provider.CreateScope();

      var first = scope.ServiceProvider.GetRequiredService<IScopedDependency>();
      var second = scope.ServiceProvider.GetRequiredService<IScopedDependency>();

      first.Should().NotBeSameAs(second);
    }
  }
}
