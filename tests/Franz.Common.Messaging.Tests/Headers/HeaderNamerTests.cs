using FluentAssertions;
using Franz.Common.Messaging;
using Xunit;

namespace Franz.Common.Messaging.Tests;

public class HeaderNamerTests
{
  // Define a dummy type within the test namespace to verify naming logic
  private class TestEvent { }

  [Fact]
  public void GetEventClassName_ShouldReturnFormattedString()
  {
    // Arrange
    var type = typeof(TestEvent);
    var expectedFullName = type.FullName;
    var expectedAssemblyName = type.Assembly.GetName().Name;
    var expectedResult = $"{expectedFullName}, {expectedAssemblyName}";

    // Act
    var result = HeaderNamer.GetEventClassName(type);

    // Assert
    result.Should().Be(expectedResult);
    result.Should().Contain(",");
    result.Should().StartWith("Franz.Common.Messaging.Tests.HeaderNamerTests+TestEvent");
  }

  [Fact]
  public void GetEventClassName_WithSystemType_ShouldFollowSamePattern()
  {
    // Arrange
    var type = typeof(string);
    var expected = "System.String, System.Private.CoreLib";

    // Act
    var result = HeaderNamer.GetEventClassName(type);

    // Assert
    // Note: In some environments, the assembly name for string might vary (mscorlib vs CoreLib),
    // so we verify the pattern.
    result.Should().Be($"{type.FullName}, {type.Assembly.GetName().Name}");
  }
}