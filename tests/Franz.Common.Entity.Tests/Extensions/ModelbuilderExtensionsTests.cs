using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.EntityFramework.Extensions;
using Franz.Common.EntityFramework.Tests.Extensions.Dummies;

namespace Franz.Common.EntityFramework.Tests.Extensions
{
  public class ModelBuilderExtensionsTests
  {
    [Fact]
    public void ConvertEnumeration_AppliesValueConverterAndRenamesColumn()
    {
      // Arrange
      var options = new DbContextOptionsBuilder<TestDbContext2>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      var dispatcher = new Mock<IDispatcher>();

      using var context = new TestDbContext2(options, dispatcher.Object);

      // Act: Accessing .Model triggers OnModelCreating and your Extension Method
      var model = context.Model;

      // Assert: Step-by-step verification to avoid NullReferenceException
      var entityType = model.FindEntityType(typeof(DummyEntity));
      Assert.True(entityType != null,
          $"Entity {nameof(DummyEntity)} not found in model. Ensure it's a DbSet in TestDbContext.");

      var property = entityType.FindProperty(nameof(DummyEntity.EnumProp));
      Assert.True(property != null,
          $"Property {nameof(DummyEntity.EnumProp)} not found on entity {nameof(DummyEntity)}.");

      // Check if the ValueConverter was applied
      var converter = property.GetValueConverter();
      Assert.True(converter != null,
          $"ValueConverter was not applied to {property.Name}. Check IsEnumerationClass logic.");

      // Check if the Column Name was renamed
      var columnName = property.GetColumnName();
      Assert.Equal("EnumPropId", columnName);
    }
  }
}