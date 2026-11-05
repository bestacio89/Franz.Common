#nullable enable
using Confluent.Kafka;
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
    var registration = coordinator.CreateRegistration("orders");
    var partition = new TopicPartition("orders", new Partition(0));
    var token = coordinator.GetRebalanceToken(registration, partition);

    coordinator.OnPartitionsRevoked(registration, new[] { partition });

    token.IsCancellationRequested.Should().BeTrue();
  }

  [Fact]
  public void PartitionsLost_CancelsCurrentHandlerToken()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var partition = new TopicPartition("orders", new Partition(0));
    var token = coordinator.GetRebalanceToken(registration, partition);

    coordinator.OnPartitionsLost(registration, new[] { partition });

    token.IsCancellationRequested.Should().BeTrue();
  }

  [Fact]
  public void PartitionsAssigned_ReplacesCancelledToken()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var registration = coordinator.CreateRegistration("orders");
    var partition = new TopicPartition("orders", new Partition(0));
    var previous = coordinator.GetRebalanceToken(registration, partition);
    coordinator.OnPartitionsRevoked(registration, new[] { partition });

    coordinator.OnPartitionsAssigned(registration, new[] { partition });

    previous.IsCancellationRequested.Should().BeTrue();
    coordinator.GetRebalanceToken("orders", partition).IsCancellationRequested.Should().BeFalse();
  }

  [Fact]
  public void RevokingOnePartition_DoesNotCancelOtherPartitionWork()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var registration = coordinator.CreateRegistration("orders");
    var revokedPartition = new TopicPartition("orders", new Partition(0));
    var retainedPartition = new TopicPartition("orders", new Partition(1));
    var revokedToken = coordinator.GetRebalanceToken(registration, revokedPartition);
    var retainedToken = coordinator.GetRebalanceToken(registration, retainedPartition);

    coordinator.OnPartitionsRevoked(registration, new[] { revokedPartition });

    revokedToken.IsCancellationRequested.Should().BeTrue();
    retainedToken.IsCancellationRequested.Should().BeFalse();
  }

  [Fact]
  public void HandlerExecutionState_TracksActiveHandler()
  {
    using var coordinator = new KafkaRebalanceCoordinator();

    var registration = coordinator.CreateRegistration("orders");
    coordinator.BeginHandlerExecution(registration);
    coordinator.IsHandlerExecutionActive(registration).Should().BeTrue();

    coordinator.EndHandlerExecution(registration);
    coordinator.IsHandlerExecutionActive(registration).Should().BeFalse();
  }
  [Fact]
  public void ConsumersInSameGroup_HaveIndependentPartitionAndHandlerState()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var firstConsumer = coordinator.CreateRegistration("orders");
    var secondConsumer = coordinator.CreateRegistration("orders");
    var partition = new TopicPartition("orders", new Partition(0));
    var firstToken = coordinator.GetRebalanceToken(firstConsumer, partition);
    var secondToken = coordinator.GetRebalanceToken(secondConsumer, partition);

    coordinator.BeginHandlerExecution(firstConsumer);
    coordinator.OnPartitionsRevoked(firstConsumer, new[] { partition });

    firstToken.IsCancellationRequested.Should().BeTrue();
    secondToken.IsCancellationRequested.Should().BeFalse();
    coordinator.IsHandlerExecutionActive(firstConsumer).Should().BeTrue();
    coordinator.IsHandlerExecutionActive(secondConsumer).Should().BeFalse();

    coordinator.EndHandlerExecution(firstConsumer);
  }
}