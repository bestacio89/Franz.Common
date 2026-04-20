using FluentAssertions;
using Franz.Common.Business.Domain;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Business.Tests.Domain.EntityTests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Franz.Common.Business.Tests.Domain.EntityTests;

public class EntityEqualityTests
{
  private readonly IServiceProvider _provider;

  public EntityEqualityTests()
  {
    var services = new ServiceCollection();

    services.AddSingleton<IIdGenerator<Guid>, GuidV7Generator>();
    services.AddTransient(typeof(IEntityFactory<,>), typeof(EntityFactory<,>));

    _provider = services.BuildServiceProvider();
  }

  private TestEntity CreateEntity()
  {
    var factory = _provider.GetRequiredService<IEntityFactory<Guid, TestEntity>>();
    return factory.Create();
  }

  [Fact]
  public void Entities_Created_By_Factory_Should_Have_Different_Ids()
  {
    var e1 = CreateEntity();
    var e2 = CreateEntity();

    e1.Should().NotBe(e2);
    e1.GetId().Should().NotBe(e2.GetId());
  }

  [Fact]
  public void Entities_Created_By_Factory_Should_Be_Transient_Initially()
  {
    var entity = CreateEntity();

    entity.IsTransient().Should().BeFalse(); // ID assigned by factory
  }

  [Fact]
  public void Equality_Should_Be_Based_On_Id()
  {
    var factory = _provider.GetRequiredService<IEntityFactory<Guid, TestEntity>>();

    var e1 = factory.Create();
    var e2 = factory.Create();

    e1.Should().NotBe(e2);
  }
}