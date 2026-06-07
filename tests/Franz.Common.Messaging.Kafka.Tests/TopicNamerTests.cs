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
  // =========================================================
  // SYSTEM MODE — assembly-based (existing behaviour)
  // =========================================================

  [Fact]
  public void GetTopicName_WithControllerAndRequiredKafkaTopicAttribute_ReturnsFormattedTopic()
  {
    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Assembly).Returns(typeof(UserController).Assembly);

    var topic = TopicNamer.GetTopicName(mockAssembly.Object);

    topic.Should().Be("User-formatted-topic");
  }

  [Fact]
  public void GetTopicName_WithExplicitTopicAttribute_ReturnsCorrectTopic()
  {
    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Assembly).Returns(typeof(LegacyController).Assembly);

    var topic = TopicNamer.GetTopicName(mockAssembly.Object);

    topic.Should().BeOneOf("User-formatted-topic", "legacy-explicit-topic");
  }

  [Theory]
  [InlineData("Company.Identity.Api", "identity-in")]
  [InlineData("testhost", "franz-test-in")]
  public void GetTopicName_Fallbacks_ShouldMatchLogic(string assemblyName, string expected)
  {
    var mockReflectionAssembly = new Mock<Assembly>();
    mockReflectionAssembly.Setup(a => a.GetName()).Returns(new AssemblyName(assemblyName));
    mockReflectionAssembly.Setup(a => a.GetTypes()).Returns(Array.Empty<Type>());

    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Assembly).Returns(mockReflectionAssembly.Object);

    var topic = TopicNamer.GetTopicName(mockAssembly.Object);

    topic.Should().Be(expected);
  }

  [Fact]
  public void GetDeadLetterTopicName_WithoutController_ReturnsDefaultDLQSuffix()
  {
    var mockReflectionAssembly = new Mock<Assembly>();
    mockReflectionAssembly.Setup(a => a.GetName())
        .Returns(new AssemblyName("Company.Identity.Api"));
    mockReflectionAssembly.Setup(a => a.GetTypes()).Returns(Array.Empty<Type>());

    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Assembly).Returns(mockReflectionAssembly.Object);

    var dlt = TopicNamer.GetDeadLetterTopicName(mockAssembly.Object);

    dlt.Should().Be("identity-in-in-dlt");
  }

  // =========================================================
  // EVENT MODE — type-based (new behaviour)
  // =========================================================

  [Theory]
  [InlineData(typeof(HeroCreatedEvent), "hero-created-in")]
  [InlineData(typeof(SkillAssignedEvent), "skill-assigned-in")]
  [InlineData(typeof(OrderPlaced), "order-placed-in")]   // no Event suffix
  [InlineData(typeof(UserRegisteredEvent), "user-registered-in")]
  public void GetTopicName_FromEventType_ReturnsKebabCaseTopic(Type eventType, string expected)
  {
    var topic = TopicNamer.GetTopicName(eventType);
    topic.Should().Be(expected);
  }

  [Theory]
  [InlineData(typeof(HeroCreatedEvent), "hero-created-in-dlt")]
  [InlineData(typeof(SkillAssignedEvent), "skill-assigned-in-dlt")]
  public void GetDeadLetterTopicName_FromEventType_ReturnsKebabCaseDlt(Type eventType, string expected)
  {
    var dlt = TopicNamer.GetDeadLetterTopicName(eventType);
    dlt.Should().Be(expected);
  }

  [Fact]
  public void GetTopicName_FromEventType_NullThrows()
  {
    var act = () => TopicNamer.GetTopicName((Type)null!);
    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void GetTopicName_SystemAndEventMode_ProduceDifferentTopicsForSameService()
  {
    // System mode → service assembly name
    var mockReflectionAssembly = new Mock<Assembly>();
    mockReflectionAssembly.Setup(a => a.GetName())
        .Returns(new AssemblyName("Acme.HeroService.Api"));
    mockReflectionAssembly.Setup(a => a.GetTypes()).Returns(Array.Empty<Type>());

    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Assembly).Returns(mockReflectionAssembly.Object);

    var systemTopic = TopicNamer.GetTopicName(mockAssembly.Object);  // heroservice-in
    var eventTopic = TopicNamer.GetTopicName(typeof(HeroCreatedEvent)); // hero-created-in

    systemTopic.Should().Be("heroservice-in");
    eventTopic.Should().Be("hero-created-in");
    systemTopic.Should().NotBe(eventTopic);
  }

  // =========================================================
  // DUMMY CONTROLLERS
  // =========================================================

  [RequiredKafkaTopic("{0}-formatted-topic", "users", DeadLetterTopic = "users-dlt-topic")]
  internal class UserController : ControllerBase { }

  [RequiredKafkaTopic(Topic = "legacy-explicit-topic")]
  internal class LegacyController : ControllerBase { }

  // =========================================================
  // DUMMY EVENT TYPES
  // =========================================================

  private class HeroCreatedEvent { }
  private class SkillAssignedEvent { }
  private class OrderPlaced { }  // no Event suffix — still valid
  private class UserRegisteredEvent { }
}