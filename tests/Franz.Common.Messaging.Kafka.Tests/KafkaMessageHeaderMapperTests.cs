#nullable enable

using System.Text;
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka;
using Franz.Common.Messaging.Messages;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests;

public sealed class KafkaMessageHeaderMapperTests
{
  [Fact]
  public void Restore_ShouldMergeNativeHeadersIntoTheFranzEnvelope()
  {
    var messageId = Guid.CreateVersion7();
    var correlationId = Guid.CreateVersion7();
    var causationId = Guid.CreateVersion7();
    var tenantId = Guid.CreateVersion7();
    var domainId = Guid.CreateVersion7();
    var message = new Message("{}") { Id = messageId };
    var headers = new Headers
    {
      { "X-Message-ID", Encode(messageId.ToString("D")) },
      { Message.CorrelationHeader, Encode(correlationId.ToString("D")) },
      { Message.CausationHeader, Encode(causationId.ToString("D")) },
      { "TenantId", Encode(tenantId.ToString("D")) },
      { "DomainId", Encode(domainId.ToString("D")) },
      { "X-Trace-Tag", Encode("one") },
      { "X-Trace-Tag", Encode("two") }
    };

    KafkaMessageHeaderMapper.Restore(message, headers);

    message.Id.Should().Be(messageId);
    message.CorrelationId.Should().Be(correlationId);
    message.Headers[Message.CausationHeader].Should().ContainSingle(causationId.ToString("D"));
    message.Headers["TenantId"].Should().ContainSingle(tenantId.ToString("D"));
    message.Headers["DomainId"].Should().ContainSingle(domainId.ToString("D"));
    message.Headers["X-Trace-Tag"].Should().Equal("one", "two");
  }

  [Fact]
  public void Restore_ShouldUseNativeIdentityWhenEnvelopeIdentityIsEmpty()
  {
    var messageId = Guid.CreateVersion7();
    var message = new Message("{}") { Id = Guid.Empty };
    var headers = new Headers
    {
      { "X-Message-ID", Encode(messageId.ToString("D")) }
    };

    KafkaMessageHeaderMapper.Restore(message, headers);

    message.Id.Should().Be(messageId);
  }

  [Fact]
  public void Restore_ShouldRejectConflictingNativeAndEnvelopeIdentity()
  {
    var message = new Message("{}") { Id = Guid.CreateVersion7() };
    var headers = new Headers
    {
      { "X-Message-ID", Encode(Guid.CreateVersion7().ToString("D")) }
    };

    var act = () => KafkaMessageHeaderMapper.Restore(message, headers);

    act.Should().Throw<InvalidDataException>()
        .WithMessage("*message identity*conflicts*");
  }

  [Fact]
  public void Restore_ShouldRejectConflictingCorrelationMetadata()
  {
    var message = new Message("{}") { CorrelationId = Guid.CreateVersion7() };
    var headers = new Headers
    {
      { Message.CorrelationHeader, Encode(Guid.CreateVersion7().ToString("D")) }
    };

    var act = () => KafkaMessageHeaderMapper.Restore(message, headers);

    act.Should().Throw<InvalidDataException>()
        .WithMessage("*correlation*conflicts*");
  }

  [Fact]
  public void Restore_ShouldRejectMultipleNativeMessageIdentities()
  {
    var messageId = Guid.CreateVersion7();
    var message = new Message("{}") { Id = messageId };
    var headers = new Headers
    {
      { "X-Message-ID", Encode(messageId.ToString("D")) },
      { "X-Message-ID", Encode(messageId.ToString("D")) }
    };

    var act = () => KafkaMessageHeaderMapper.Restore(message, headers);

    act.Should().Throw<InvalidDataException>()
        .WithMessage("*exactly one*message identity*");
  }

  private static byte[] Encode(string value) => Encoding.UTF8.GetBytes(value);
}
