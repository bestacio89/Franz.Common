#nullable enable
using Franz.Common.Annotations;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Reflection;
using Microsoft.AspNetCore.Mvc;
using System;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests
{
  public class TopicNamerTests
  {
    [Fact]
    public void GetTopicName_WithControllerAndRequiredKafkaTopicAttribute_ReturnsFormattedTopic()
    {
      // Arrange
      var assembly = new FakeAssemblyWithController();

      // Act
      var topic = TopicNamer.GetTopicName(assembly);

      // Assert
      Assert.Equal("users-formatted-topic-in", topic);
    }

    [Fact]
    public void GetDeadLetterTopicName_WithControllerAndAttribute_ReturnsDeadLetterTopic()
    {
      // Arrange
      var assembly = new FakeAssemblyWithController();

      // Act
      var dlt = TopicNamer.GetDeadLetterTopicName(assembly);

      // Assert
      Assert.Equal("users-dlt-topic", dlt);
    }

    [Fact]
    public void GetTopicName_WithoutController_ReturnsDefaultTopic()
    {
      // Arrange
      var assembly = new FakeAssemblyWithoutController("Company.Service.Api");

      // Act
      var topic = TopicNamer.GetTopicName(assembly);

      // Assert
      Assert.Equal("service-in", topic);
    }

    [Fact]
    public void GetDeadLetterTopicName_WithoutController_ReturnsDefaultDLQ()
    {
      // Arrange
      var assembly = new FakeAssemblyWithoutController("Company.Service.Api");

      // Act
      var dlt = TopicNamer.GetDeadLetterTopicName(assembly);

      // Assert
      Assert.Equal("service-in-in-dlt", dlt);
    }

    [Fact]
    public void GetTopicName_WithTestHostAssembly_ReturnsFranzTest()
    {
      // Arrange
      var assembly = new FakeAssemblyWithoutController("testhost");

      // Act
      var topic = TopicNamer.GetTopicName(assembly);

      // Assert
      Assert.Equal("franz-test-in", topic);
    }
  }

  // Fake assembly and controllers to simulate attribute scenarios
  internal class FakeAssemblyWithController : IAssembly
  {
    public System.Reflection.Assembly Assembly { get; }

    public string? Name => "Company.Users.Api";
    public string? FullName => "Company.Users.Api, Version=1.0.0.0";

    public FakeAssemblyWithController()
    {
      Assembly = typeof(DummyController).Assembly;
    }

    // Dummy controller with Kafka attribute
    [RequiredKafkaTopic("{0}-formatted-topic", "users", DeadLetterTopic = "users-dlt-topic")]
    internal class DummyController : ControllerBase { }
  }

  internal class FakeAssemblyWithoutController : IAssembly
  {
    public System.Reflection.Assembly Assembly { get; }

    public string? Name { get; }
    public string? FullName => Name;

    public FakeAssemblyWithoutController(string name)
    {
      Name = name;
      Assembly = typeof(FakeAssemblyWithoutController).Assembly;
    }
  }
}