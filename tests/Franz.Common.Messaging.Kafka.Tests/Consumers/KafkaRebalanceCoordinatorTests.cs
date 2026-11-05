#nullable enable
using FluentAssertions;
using Franz.Common.Messaging.Kafka;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers;

public sealed class KafkaRebalanceCoordinatorTests
{
  [Fact]
  public void PartitionsRevoked_CancelsCurrentHandlerToken()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var token = coordinator.GetRebalanceToken("orders");

    coordinator.OnPartitionsRevoked("orders");

    token.IsCancellationRequested.Should().BeTrue();
  }

  [Fact]
  public void PartitionsLost_CancelsCurrentHandlerToken()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var token = coordinator.GetRebalanceToken("orders");

    coordinator.OnPartitionsLost("orders");

    token.IsCancellationRequested.Should().BeTrue();
  }

  [Fact]
  public void PartitionsAssigned_ReplacesCancelledToken()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var previous = coordinator.GetRebalanceToken("orders");
    coordinator.OnPartitionsRevoked("orders");

    coordinator.OnPartitionsAssigned("orders");

    previous.IsCancellationRequested.Should().BeTrue();
    coordinator.GetRebalanceToken("orders").IsCancellationRequested.Should().BeFalse();
  }

  [Fact]
  public void HandlerExecutionState_TracksActiveHandler()
  {
    using var coordinator = new KafkaRebalanceCoordinator();

    coordinator.BeginHandlerExecution("orders");
    coordinator.IsHandlerExecutionActive("orders").Should().BeTrue();

    coordinator.EndHandlerExecution("orders");
    coordinator.IsHandlerExecutionActive("orders").Should().BeFalse();
  }
}