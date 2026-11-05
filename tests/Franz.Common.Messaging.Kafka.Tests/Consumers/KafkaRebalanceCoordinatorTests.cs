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
    var partition = new TopicPartition("orders", new Partition(0));
    var token = coordinator.GetRebalanceToken("orders", partition);

    coordinator.OnPartitionsRevoked("orders", new[] { partition });

    token.IsCancellationRequested.Should().BeTrue();
  }

  [Fact]
  public void PartitionsLost_CancelsCurrentHandlerToken()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var partition = new TopicPartition("orders", new Partition(0));
    var token = coordinator.GetRebalanceToken("orders", partition);

    coordinator.OnPartitionsLost("orders", new[] { partition });

    token.IsCancellationRequested.Should().BeTrue();
  }

  [Fact]
  public void PartitionsAssigned_ReplacesCancelledToken()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var partition = new TopicPartition("orders", new Partition(0));
    var previous = coordinator.GetRebalanceToken("orders", partition);
    coordinator.OnPartitionsRevoked("orders", new[] { partition });

    coordinator.OnPartitionsAssigned("orders", new[] { partition });

    previous.IsCancellationRequested.Should().BeTrue();
    coordinator.GetRebalanceToken("orders", partition).IsCancellationRequested.Should().BeFalse();
  }

  [Fact]
  public void RevokingOnePartition_DoesNotCancelOtherPartitionWork()
  {
    using var coordinator = new KafkaRebalanceCoordinator();
    var revokedPartition = new TopicPartition("orders", new Partition(0));
    var retainedPartition = new TopicPartition("orders", new Partition(1));
    var revokedToken = coordinator.GetRebalanceToken("orders", revokedPartition);
    var retainedToken = coordinator.GetRebalanceToken("orders", retainedPartition);

    coordinator.OnPartitionsRevoked("orders", new[] { revokedPartition });

    revokedToken.IsCancellationRequested.Should().BeTrue();
    retainedToken.IsCancellationRequested.Should().BeFalse();
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