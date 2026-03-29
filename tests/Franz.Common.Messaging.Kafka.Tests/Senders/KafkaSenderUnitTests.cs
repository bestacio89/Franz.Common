using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Franz.Common.Messaging.Kafka.Senders;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Senders;

[Collection("KafkaSender")]
public sealed class KafkaSenderUnitTests
{
  private static KafkaSender BuildSender(string bootstrapServers = "localhost:9092")
  {
    var mockAssembly = new Mock<IAssembly>();
    mockAssembly.Setup(a => a.Name).Returns("Company.Test.Api");
    mockAssembly.Setup(a => a.Assembly).Returns(typeof(KafkaSender).Assembly);

    var mockAccessor = new Mock<IAssemblyAccessor>();
    mockAccessor.Setup(a => a.GetEntryAssembly()).Returns(mockAssembly.Object);

    return new KafkaSender(
        Options.Create(new KafkaMessagingOptions { BootStrapServers = bootstrapServers }),
        new JsonMessageSerializer(),
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
    // the underlying producer is already disposed on the first call.
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