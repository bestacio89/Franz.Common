#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Senders;

[Collection("KafkaSender")]
public sealed class KafkaSenderUnitTests
{
  private static KafkaSender BuildSender()
  {
    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Name).Returns("Company.Test.Api");

    var mockAccessor = new Mock<IAssemblyAccessor>();
    mockAccessor.Setup(a => a.GetEntryAssembly()).Returns(mockAssembly.Object);

    // Deep Refactoring: The Sender no longer requires IOptions<KafkaMessagingOptions>.
    // We mock the IProducer natively, confirming the DI container's ownership of the transport lifecycle.
    var mockProducer = new Mock<IProducer<string, byte[]>>();

    var mockSerializer = new Mock<IMessageSerializer>();
    mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");

    return new KafkaSender(
        mockProducer.Object,
        mockSerializer.Object,
        mockAccessor.Object,
        NullLogger<KafkaSender>.Instance);
  }

  [Fact]
  public async Task SendAsync_ShouldThrow_WhenMessageIsNull()
  {
    await using var sender = BuildSender();
    var act = async () => await sender.SendAsync(null!);
    await act.Should().ThrowAsync<ArgumentNullException>();
  }

  [Fact]
  public void Dispose_ShouldNotThrow_WhenCalledOnce()
  {
    var sender = BuildSender();
    var act = () => sender.Dispose();
    act.Should().NotThrow();
  }

  [Fact]
  public void Dispose_ShouldNotThrow_WhenCalledTwice()
  {
    // Idempotent dispose — calling twice must not throw even though
    // the underlying producer lifecycle is now managed by the DI container.
    var sender = BuildSender();
    sender.Dispose();
    var act = () => sender.Dispose();
    act.Should().NotThrow();
  }

  [Fact]
  public async Task DisposeAsync_ShouldNotThrow_WhenCalledOnce()
  {
    var sender = BuildSender();
    var act = async () => await sender.DisposeAsync();
    await act.Should().NotThrowAsync();
  }

  [Fact]
  public async Task DisposeAsync_ShouldNotThrow_WhenCalledTwice()
  {
    var sender = BuildSender();
    await sender.DisposeAsync();
    var act = async () => await sender.DisposeAsync();
    await act.Should().NotThrowAsync();
  }
}