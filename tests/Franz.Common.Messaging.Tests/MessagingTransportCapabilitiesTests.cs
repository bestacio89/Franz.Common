using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Xunit;

namespace Franz.Common.Messaging.Tests;

public sealed class MessagingTransportCapabilitiesTests
{
  [Fact]
  public void MessagingTransportCapabilities_ShouldRejectContradictoryDeclarations()
  {
    var action = () => new MessagingTransportCapabilities(
        "test",
        new[] { MessagingCapability.Publish },
        new Dictionary<MessagingCapability, string>
        {
          [MessagingCapability.Publish] = "Not supported."
        });

    action.Should().Throw<ArgumentException>()
        .WithMessage("*both supported and unsupported*");
  }

  [Fact]
  public void MessagingTransportCapabilities_ShouldRequireReasonsForUnsupportedCapabilities()
  {
    var action = () => new MessagingTransportCapabilities(
        "test",
        Array.Empty<MessagingCapability>(),
        new Dictionary<MessagingCapability, string>
        {
          [MessagingCapability.Publish] = string.Empty
        });

    action.Should().Throw<ArgumentException>()
        .WithMessage("*requires an explanation*");
  }
}
