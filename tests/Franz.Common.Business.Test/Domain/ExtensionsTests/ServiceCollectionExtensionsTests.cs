using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Business.Extensions;
using Franz.Common.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Franz.Common.Business.Tests.Domain.ExtensionsTests;

public sealed class BusinessBootstrapExtensionsTests
{
  [Fact]
  public void AddBusiness_Should_Register_Core_Domain_Services()
  {
    var services = new ServiceCollection();
    var assembly = typeof(BusinessBootstrapExtensionsTests).Assembly;

    services.AddBusiness(assembly);

    var provider = services.BuildServiceProvider();

    provider.GetService<IIdGenerator<Guid>>()
        .Should()
        .NotBeNull();

    provider.GetService<IEntityFactory<Guid, DummyEntity>>()
        .Should()
        .NotBeNull();
  }

  [Fact]
  public void AddBusiness_Should_Not_Throw()
  {
    var services = new ServiceCollection();
    var assembly = typeof(BusinessBootstrapExtensionsTests).Assembly;

    Action act = () => services.AddBusiness(assembly);

    act.Should().NotThrow();
  }

  [Fact]
  public void AddBusinessPlatform_Should_Return_Same_ServiceCollection()
  {
    var services = new ServiceCollection();

    var result = services.AddBusinessPlatform();

    result.Should().BeSameAs(services);
  }

  [Fact]
  public void TryAddBusiness_Should_Handle_Null_Assembly()
  {
    var services = new ServiceCollection();

    var result = services.TryAddBusiness(null);

    result.Should().BeSameAs(services);
  }

  [Fact]
  public void TryAddBusiness_Should_Not_Throw_With_Valid_Assembly()
  {
    var services = new ServiceCollection();
    var assembly = typeof(BusinessBootstrapExtensionsTests).Assembly;

    Action act = () => services.TryAddBusiness(assembly);

    act.Should().NotThrow();
  }

  // Minimal dummy entity for DI resolution tests
  private sealed class DummyEntity : Entity<Guid>
  {
    public DummyEntity() : base(Guid.NewGuid()) { }
  }
}