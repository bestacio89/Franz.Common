using FluentAssertions;
using Franz.Common.Messaging.Configuration;
using Xunit;

namespace Franz.Common.Messaging.Tests.Configuration;

public class MessagingOptionsTests
{
  [Fact]
  public void MessagingOptions_ShouldHaveDefaultValues()
  {
    // Arrange & Act
    var options = new MessagingOptions();

    // Assert
    options.BootStrapServers.Should().Be("localhost:9092");
    options.GroupID.Should().BeEmpty();
    options.SslEnabled.Should().BeNull();
    options.Port.Should().BeNull();
  }

  [Fact]
  public void MessagingOptions_ShouldSetAndGetProperties()
  {
    // Arrange
    var options = new MessagingOptions();
    var expectedHost = "rabbitmq.local";
    var expectedPort = 5672;
    var expectedGroupId = "orders-service-group";

    // Act
    options.HostName = expectedHost;
    options.Port = expectedPort;
    options.GroupID = expectedGroupId;
    options.SslEnabled = true;

    // Assert
    options.HostName.Should().Be(expectedHost);
    options.Port.Should().Be(expectedPort);
    options.GroupID.Should().Be(expectedGroupId);
    options.SslEnabled.Should().BeTrue();
  }

  [Fact]
  public void MessagingOptions_SslLocations_ShouldBeAssignable()
  {
    // Arrange
    var options = new MessagingOptions();
    var path = "/etc/ssl/certs/ca.pem";

    // Act
    options.SslCaLocation = path;
    options.SslCertificateLocation = path;
    options.SslKeyLocation = path;

    // Assert
    options.SslCaLocation.Should().Be(path);
    options.SslCertificateLocation.Should().Be(path);
    options.SslKeyLocation.Should().Be(path);
  }
}