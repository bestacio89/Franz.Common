using FluentAssertions;
using Franz.Common.Business.Domain.Factories;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Business.Tests.Domain.EntityTests;
using Xunit;
namespace Franz.Common.Business.Tests.Domain.EntityTests;

public class EntityFactoryTests
{
  [Fact]
  public void Factory_Should_Create_Entity_With_GuidV7_Id()
  {
    // Arrange
    var generator = new GuidV7Generator();
    var factory = new EntityFactory<Guid, TestEntity>(generator);

    // Act
    var entity = factory.Create();

    // Assert
    entity.Id.Should().NotBe(Guid.Empty);
  }
}