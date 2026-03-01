using System.Text.Json;
using FluentAssertions;
using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Messages;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Tests.Factories;

public class MessagingFactorySuiteTests
{
  #region Mocks and Dummies
  private record TestIntegrationEvent(string EventId) : IIntegrationEvent;
  private record TestQuery(int Id) : IQuery<string>;
  private record InvalidType(string Data);
  #endregion

  #region IntegrationEventMessageBuilderStrategy Tests
  [Fact]
  public void IntegrationEventStrategy_CanBuild_ShouldReturnTrueForIIntegrationEvent()
  {
    var strategy = new IntegrationEventMessageBuilderStrategy();
    var @event = new TestIntegrationEvent("EVT-123");

    strategy.CanBuild(@event).Should().BeTrue();
  }

  [Fact]
  public void IntegrationEventStrategy_Build_ShouldSetCorrectMetadata()
  {
    var strategy = new IntegrationEventMessageBuilderStrategy();
    var @event = new TestIntegrationEvent("EVT-123");

    var result = strategy.Build(@event);

    result.Kind.Should().Be(MessageKind.IntegrationEvent);
    result.Headers[MessagingConstants.ClassName].Should().BeEquivalentTo(new StringValues(HeaderNamer.GetEventClassName(typeof(TestIntegrationEvent))));
  }
  #endregion

  #region QueryMessageBuilderStrategy Tests
  [Fact]
  public void QueryStrategy_CanBuild_ShouldIdentifyGenericIQuery()
  {
    var strategy = new QueryMessageBuilderStrategy();
    var query = new TestQuery(1);

    strategy.CanBuild(query).Should().BeTrue();
  }

  [Fact]
  public void QueryStrategy_Build_ShouldThrowIfTypeIsInvalid()
  {
    var strategy = new QueryMessageBuilderStrategy();
    var invalid = new InvalidType("No");

    Action act = () => strategy.Build(invalid);

    act.Should().Throw<InvalidOperationException>();
  }
  #endregion

  #region MessageFactory Tests
  [Fact]
  public void MessageFactory_Build_ShouldUseCorrectStrategy()
  {
    // Arrange
    var mockStrategy = new Mock<IMessageBuilderStrategy>();
    var input = new TestIntegrationEvent("123");
    var expectedMessage = new Message("{}");

    mockStrategy.Setup(s => s.CanBuild(input)).Returns(true);
    mockStrategy.Setup(s => s.Build(input)).Returns(expectedMessage);

    var factory = new MessageFactory(new[] { mockStrategy.Object });

    // Act
    var result = factory.Build(input);

    // Assert
    result.Should().BeSameAs(expectedMessage);
    mockStrategy.Verify(s => s.Build(input), Times.Once);
  }

  [Fact]
  public void MessageFactory_Build_WhenNoStrategyFound_ShouldThrowTechnicalException()
  {
    // Arrange
    var factory = new MessageFactory(Enumerable.Empty<IMessageBuilderStrategy>());
    var input = new { Data = "Nothing" };

    // Act
    Action act = () => factory.Build(input);

    // Assert
    act.Should().Throw<TechnicalException>();
  }

  [Fact]
  public void MessageFactory_Build_WhenMultipleStrategiesMatch_ShouldThrowInvalidOperationException()
  {
    // Arrange - SingleOrDefault throws if more than one matches
    var strategy1 = new Mock<IMessageBuilderStrategy>();
    var strategy2 = new Mock<IMessageBuilderStrategy>();
    strategy1.Setup(s => s.CanBuild(It.IsAny<object>())).Returns(true);
    strategy2.Setup(s => s.CanBuild(It.IsAny<object>())).Returns(true);

    var factory = new MessageFactory(new[] { strategy1.Object, strategy2.Object });

    // Act
    Action act = () => factory.Build(new object());

    // Assert
    act.Should().Throw<InvalidOperationException>();
  }
  #endregion
}