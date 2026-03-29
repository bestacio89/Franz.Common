#nullable enable
using FluentAssertions;
using Franz.Common.Headers;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Messages;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Tests.Headers;

public class HeaderPropagationMessageBuilderTests
{
  private readonly Mock<IHeaderContextAccessor> _mockAccessor;

  public HeaderPropagationMessageBuilderTests()
  {
    _mockAccessor = new Mock<IHeaderContextAccessor>();
  }

  [Fact]
  public void CanBuild_ShouldReturnTrue_WhenOptionsHaveHeaders()
  {
    // Arrange
    var options = new HeaderPropagationOptions();
    options.Headers.Add("X-Tenant-Id");
    var builder = new HeaderPropagationMessageBuilder(_mockAccessor.Object, null, options);

    // Act & Assert
    builder.CanBuild(new Message("{}")).Should().BeTrue();
  }

  [Fact]
  public void CanBuild_ShouldReturnTrue_WhenRegistrerHasHeaders()
  {
    // Arrange
    var settings = new List<IHeaderPropagationSetting> { new HeaderPropagationSetting("X-Correlation-ID") };
    var registrar = new HeaderPropagationRegister(settings);
    var builder = new HeaderPropagationMessageBuilder(_mockAccessor.Object, registrar);

    // Act & Assert
    builder.CanBuild(new Message("{}")).Should().BeTrue();
  }



  [Theory]
  [InlineData("message-id")]
  [InlineData("correlation-id")]
  [InlineData("message-type")]
  public void Build_ShouldProtectSystemInvariants(string systemHeader)
  {
    // Arrange
    var options = new HeaderPropagationOptions();
    options.Headers.Add(systemHeader);

    var builder = new HeaderPropagationMessageBuilder(_mockAccessor.Object, null, options);
    var message = new Message("{}");

    string[]? malicious = ["overwrite-attempt"];
    _mockAccessor.Setup(a => a.TryGetValue(systemHeader, out malicious)).Returns(true);

    // Act
    builder.BuildAsync(message);

    // Assert
    // System invariants should not be injectable via propagation
    message.Headers.ContainsKey(systemHeader).Should().BeFalse();
  }

  [Fact]
  public void Build_ShouldNotOverwriteExistingMessageHeaders()
  {
    // Arrange
    var options = new HeaderPropagationOptions();
    options.Headers.Add("X-Custom");

    var builder = new HeaderPropagationMessageBuilder(_mockAccessor.Object, null, options);
    var message = new Message("{}");

    // SENIOR FIX: Correctly initialize headers as string arrays
    message.Headers.Add("X-Custom", ["Original"]);

    string[]? propagated = ["New"];
    _mockAccessor.Setup(a => a.TryGetValue("X-Custom", out propagated)).Returns(true);

    // Act
    builder.BuildAsync(message);

    // Assert
    // Standard indexer access for checking values
    message.Headers["X-Custom"][0].Should().Be("Original");
  }
}