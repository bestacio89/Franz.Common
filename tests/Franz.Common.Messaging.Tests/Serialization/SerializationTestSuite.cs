using FluentAssertions;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace Franz.Common.Messaging.Tests.Serialization;

public class SerializationTestSuite
{
  private readonly IMessageSerializer _serializer = new JsonMessageSerializer();

  // A dummy record to test serialization behavior
  private record TestMessage(string FullName, int? Age = null);

  [Fact]
  public void Serialize_ShouldApplyCamelCaseAndIgnoreNulls()
  {
    // Arrange
    var message = new TestMessage("Franz Kafka", null);

    // Act
    var json = _serializer.Serialize(message);

    // Assert
    // 1. Check CamelCase: "FullName" -> "fullName"
    json.Should().Contain("fullName");
    // 2. Check IgnoreNull: "Age" should not be in the string at all
    json.Should().NotContain("age");
    json.Should().NotContain("null");
  }

  [Fact]
  public void Deserialize_GenericAndNonGeneric_ShouldBeConsistent()
  {
    // Arrange
    var json = "{\"fullName\":\"Franz Kafka\",\"age\":40}";

    // Act
    var genericResult = _serializer.Deserialize<TestMessage>(json);
    var nonGenericResult = _serializer.Deserialize(json, typeof(TestMessage)) as TestMessage;

    // Assert
    genericResult.Should().NotBeNull();
    genericResult!.FullName.Should().Be("Franz Kafka");
    genericResult.Age.Should().Be(40);

    nonGenericResult.Should().BeEquivalentTo(genericResult);
  }

  [Fact]
  public void AddDefaultMessageSerializer_ShouldRegisterAsSingleton()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddDefaultMessageSerializer();
    var provider = services.BuildServiceProvider();
    var instance1 = provider.GetService<IMessageSerializer>();
    var instance2 = provider.GetService<IMessageSerializer>();

    // Assert
    instance1.Should().BeOfType<JsonMessageSerializer>();
    instance1.Should().BeSameAs(instance2); // Singleton check
  }
}