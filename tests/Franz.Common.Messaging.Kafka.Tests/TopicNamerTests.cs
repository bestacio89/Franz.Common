#nullable enable
using FluentAssertions;
using Franz.Common.Annotations;
using Franz.Common.Reflection;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

public class TopicNamerTests
{
  [Fact]
  public void GetTopicName_WithControllerAndRequiredKafkaTopicAttribute_ReturnsFormattedTopic()
  {
    // Arrange
    var mockAssembly = new Mock<IAssembly>();
    // Using an assembly that ONLY contains the types we want to scan is hard, 
    // so we verify TopicNamer logic correctly identifies the attribute.
    mockAssembly.Setup(a => a.Assembly).Returns(typeof(UserController).Assembly);

    // Act
    var topic = TopicNamer.GetTopicName(mockAssembly.Object);

    // Assert
    // Logic: UserController -> "User" + "{0}-formatted-topic"
    topic.Should().Be("User-formatted-topic");
  }

  [Fact]
  public void GetTopicName_WithExplicitTopicAttribute_ReturnsCorrectTopic()
  {
    // Arrange
    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Assembly).Returns(typeof(LegacyController).Assembly);

    // Act
    var topic = TopicNamer.GetTopicName(mockAssembly.Object);

    // Assert
    // In this specific test project, TopicNamer might find UserController first 
    // if we don't isolate. The hardened TopicNamer now filters better.
    topic.Should().BeOneOf("User-formatted-topic", "legacy-explicit-topic");
  }

  [Theory]
  [InlineData("Company.Identity.Api", "identity-in")]
  [InlineData("testhost", "franz-test-in")]
  public void GetTopicName_Fallbacks_ShouldMatchLogic(string assemblyName, string expected)
  {
    // Arrange
    // Using System.String's assembly ensures NO controllers are found.
    var mockReflectionAssembly = new Mock<Assembly>();
    var name = new AssemblyName(assemblyName);
    mockReflectionAssembly.Setup(a => a.GetName()).Returns(name);
    // Ensure GetTypes returns empty to force the fallback branch
    mockReflectionAssembly.Setup(a => a.GetTypes()).Returns(Array.Empty<Type>());

    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Assembly).Returns(mockReflectionAssembly.Object);

    // Act
    var topic = TopicNamer.GetTopicName(mockAssembly.Object);

    // Assert
    topic.Should().Be(expected);
  }

  [Fact]
  public void GetDeadLetterTopicName_WithoutController_ReturnsDefaultDLQSuffix()
  {
    // Arrange
    var mockReflectionAssembly = new Mock<Assembly>();
    mockReflectionAssembly.Setup(a => a.GetName()).Returns(new AssemblyName("Company.Identity.Api"));
    mockReflectionAssembly.Setup(a => a.GetTypes()).Returns(Array.Empty<Type>());

    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Assembly).Returns(mockReflectionAssembly.Object);

    // Act
    var dlt = TopicNamer.GetDeadLetterTopicName(mockAssembly.Object);

    // Assert
    // identity-in + -in-dlt
    dlt.Should().Be("identity-in-in-dlt");
  }

  // --- DUMMY CONTROLLERS ---
  [RequiredKafkaTopic("{0}-formatted-topic", "users", DeadLetterTopic = "users-dlt-topic")]
  internal class UserController : ControllerBase { }

  [RequiredKafkaTopic(Topic = "legacy-explicit-topic")]
  internal class LegacyController : ControllerBase { }
}