using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Business.Tests.Domain.ExtensionsTests;

using FluentAssertions;
using Franz.Common.Business.Extensions;
using Franz.Common.Errors;
using Franz.Common.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
  [Fact]
  public void AddBusinessWithMediator_Should_Fail_When_Application_Assembly_Not_Found()
  {
    var services = new ServiceCollection();
    var entryAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;

    Action act = () => services.AddBusinessWithMediator(entryAssembly);

    act.Should().Throw<Exception>()
       .Where(e => e.GetBaseException() is TechnicalException);
  }

  [Fact]
  public void TryAddBusinessWithMediator_Should_Not_Throw_When_Application_Assembly_Not_Found()
  {
    var services = new ServiceCollection();
    var entryAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;

    Action act = () =>
        services.TryAddBusinessWithMediator(entryAssembly);

    act.Should().NotThrow();
  }

  [Fact]
  public void AddBusinessWithMediator_Should_Register_Services_When_Application_Assembly_Exists()
  {
    var services = new ServiceCollection();

    // Simulate Product.Application
    var entryAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;
    var productName = string.Join(".", entryAssembly.GetName().Name!.Split(".").Take(2));
    TestAssemblyHelper.LoadFakeApplicationAssembly($"{productName}.Application");

    Action act = () =>
        services.AddBusinessWithMediator(entryAssembly, _ => { });

    act.Should().NotThrow();
  }

  [Fact]
  public void TryAddBusinessWithMediator_Should_Delegate_To_Add_When_Assembly_Exists()
  {
    var services = new ServiceCollection();
    var entryAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;
    var productName = string.Join(".", entryAssembly.GetName().Name!.Split(".").Take(2));

    TestAssemblyHelper.LoadFakeApplicationAssembly($"{productName}.Application");

    Action act = () =>
        services.TryAddBusinessWithMediator(entryAssembly, _ => { });

    act.Should().NotThrow();
  }

  [Fact]
  public void AddFranzPlatform_Should_Not_Throw_And_Return_ServiceCollection()
  {
    var services = new ServiceCollection();
    var entryAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;
    var productName = string.Join(".", entryAssembly.GetName().Name!.Split(".").Take(2));

    TestAssemblyHelper.LoadFakeApplicationAssembly($"{productName}.Application");

    var result = services.AddFranzPlatform(entryAssembly);

    result.Should().BeSameAs(services);
  }
}

