#nullable enable
using FluentAssertions;
using Franz.Common.Reflection;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

public sealed class TopicNamerTests
{
  [Theory]
  [InlineData("Company.Identity.Api", "identity-in")]
  [InlineData("testhost", "franz-test-in")]
  [InlineData("Acme.HeroService.Api", "heroservice-in")]
  public void GetTopicName_UsesAssemblyIdentityWithoutRuntimeTypeScanning(string assemblyName, string expected)
  {
    var assembly = new Mock<IAssembly>();
    assembly.Setup(value => value.Name).Returns(assemblyName);
    TopicNamer.GetTopicName(assembly.Object).Should().Be(expected);
  }

  [Fact]
  public void GetTopicName_RejectsAssemblyWithoutName()
  {
    var assembly = new Mock<IAssembly>();
    assembly.Setup(value => value.Name).Returns((string?)null);
    var act = () => TopicNamer.GetTopicName(assembly.Object);
    act.Should().Throw<InvalidOperationException>().WithMessage("*no valid name*");
  }

  [Fact]
  public void GetDeadLetterTopicName_UsesServiceTopicAndDeadLetterSuffix()
  {
    var assembly = new Mock<IAssembly>();
    assembly.Setup(value => value.Name).Returns("Company.Identity.Api");
    TopicNamer.GetDeadLetterTopicName(assembly.Object).Should().Be("identity-in-in-dlt");
  }

  [Theory]
  [InlineData(typeof(HeroCreatedEvent), "hero-created-in")]
  [InlineData(typeof(SkillAssignedEvent), "skill-assigned-in")]
  [InlineData(typeof(OrderPlaced), "order-placed-in")]
  [InlineData(typeof(UserRegisteredEvent), "user-registered-in")]
  public void GetTopicName_FromEventType_ReturnsGoldenTopicName(Type eventType, string expected)
      => TopicNamer.GetTopicName(eventType).Should().Be(expected);

  [Theory]
  [InlineData(typeof(HeroCreatedEvent), "hero-created-in-dlt")]
  [InlineData(typeof(SkillAssignedEvent), "skill-assigned-in-dlt")]
  public void GetDeadLetterTopicName_FromEventType_ReturnsGoldenDeadLetterTopicName(Type eventType, string expected)
      => TopicNamer.GetDeadLetterTopicName(eventType).Should().Be(expected);

  [Fact]
  public void GetTopicName_FromEventType_RejectsNull()
  {
    var act = () => TopicNamer.GetTopicName((Type)null!);
    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void SystemAndEventModes_ProduceDifferentGoldenNames()
  {
    var assembly = new Mock<IAssembly>();
    assembly.Setup(value => value.Name).Returns("Acme.HeroService.Api");
    TopicNamer.GetTopicName(assembly.Object).Should().Be("heroservice-in");
    TopicNamer.GetTopicName(typeof(HeroCreatedEvent)).Should().Be("hero-created-in");
  }

  private sealed class HeroCreatedEvent { }
  private sealed class SkillAssignedEvent { }
  private sealed class OrderPlaced { }
  private sealed class UserRegisteredEvent { }
}