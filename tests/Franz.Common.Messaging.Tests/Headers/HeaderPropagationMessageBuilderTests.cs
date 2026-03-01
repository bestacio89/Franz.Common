using FluentAssertions;
using Franz.Common.Headers;
using Franz.Common.Messaging.Headers;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Franz.Common.Messaging.Tests.Headers;

public class HeaderPropagationMessageBuilderTests
{
  private readonly Mock<IHeaderContextAccessor> _mockAccessor;
  private readonly Mock<IHeaderPropagationRegistrer> _mockRegistrer;

  public HeaderPropagationMessageBuilderTests()
  {
    _mockAccessor = new Mock<IHeaderContextAccessor>();
    _mockRegistrer = new Mock<IHeaderPropagationRegistrer>(); // We'll mock the interface
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

  [Fact]
  public void Build_ShouldPropagateFromBothOptionsAndRegistrer()
  {
    // Arrange
    var options = new HeaderPropagationOptions();
    options.Headers.Add("X-Option-Header");

    var settings = new List<IHeaderPropagationSetting> { new HeaderPropagationSetting("X-Reg-Header") };
    var registrar = new HeaderPropagationRegister(settings);

    var builder = new HeaderPropagationMessageBuilder(_mockAccessor.Object, registrar, options);
    var message = new Message("{}");

    var val1 = new StringValues("val-opt");
    var val2 = new StringValues("val-reg");

    _mockAccessor.Setup(a => a.TryGetValue("X-Option-Header", out val1)).Returns(true);
    _mockAccessor.Setup(a => a.TryGetValue("X-Reg-Header", out val2)).Returns(true);

    // Act
    builder.Build(message);

    // Assert
    message.Headers["X-Option-Header"].Should().BeEquivalentTo(val1);
    message.Headers["X-Reg-Header"].Should().BeEquivalentTo(val2);
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

    var malicious = new StringValues("overwrite-attempt");
    _mockAccessor.Setup(a => a.TryGetValue(systemHeader, out malicious)).Returns(true);

    // Act
    builder.Build(message);

    // Assert
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
    message.Headers.Add("X-Custom", "Original");

    var propagated = new StringValues("New");
    _mockAccessor.Setup(a => a.TryGetValue("X-Custom", out propagated)).Returns(true);

    // Act
    builder.Build(message);

    // Assert
    message.Headers["X-Custom"].ToString().Should().Be("Original");
  }
}