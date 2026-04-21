#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Messages;
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

    Func<Task> act = async () => await sender.SendAsync(null!);

    await act.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("message");
  }

  [Fact]
  public async Task SendAsync_ShouldThrow_WhenDisposed()
  {
    var sender = BuildSender();

    await sender.DisposeAsync();

    Func<Task> act = async () =>
      await sender.SendAsync(new Message { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Body = "test" });

    await act.Should().ThrowAsync<ObjectDisposedException>();
  }

  [Fact]
  public void Dispose_ShouldBeIdempotent()
  {
    var sender = BuildSender();

    sender.Dispose();
    sender.Dispose();

    true.Should().BeTrue();
  }

  [Fact]
  public async Task DisposeAsync_ShouldBeIdempotent()
  {
    var sender = BuildSender();

    await sender.DisposeAsync();
    await sender.DisposeAsync();

    true.Should().BeTrue();
  }
}