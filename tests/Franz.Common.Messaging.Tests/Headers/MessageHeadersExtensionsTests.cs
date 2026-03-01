using FluentAssertions;
using Franz.Common.Errors;
using Franz.Common.Headers;
using Franz.Common.Messaging.Headers;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Franz.Common.Messaging.Tests.Headers;

public class MessageHeadersExtensionsTests
{
  [Fact]
  public void TryGetMessageId_ShouldReturnTrue_WhenHeaderExists()
  {
    // Arrange
    var headers = new MessageHeaders();
    var messageId = Guid.NewGuid();
    headers.Add(MessagingConstants.MessageId, messageId.ToString());

    // Act
    var success = headers.TryGetMessageId(out var result);

    // Assert
    success.Should().BeTrue();
    result.Should().Be(messageId);
  }

  [Fact]
  public void GetClassName_ShouldThrowTechnicalException_WhenHeaderIsMissing()
  {
    // Arrange
    var headers = new MessageHeaders();

    // Act
    Action act = () => headers.GetClassName();

    // Assert
    act.Should().Throw<TechnicalException>();
  }

  [Fact]
  public void TryGetIdentityRoles_ShouldReturnAllRoles()
  {
    // Arrange
    var headers = new MessageHeaders();
    var roles = new[] { "Admin", "Developer" };
    headers.SetIdentityRoles(roles);

    // Act
    var success = headers.TryGetIdentityRoles(out var result);

    // Assert
    success.Should().BeTrue();
    result.Should().BeEquivalentTo(roles);
  }

  [Fact]
  public void Setters_ShouldPopulateCorrectConstants()
  {
    // Arrange
    var headers = new MessageHeaders();
    var tenantId = Guid.NewGuid();
    var email = "captain@franz.com";

    // Act
    headers.SetTenantId(tenantId);
    headers.SetIdentityEmail(email);

    // Assert
    headers[HeaderConstants.TenantId].ToString().Should().Be(tenantId.ToString());
    headers[HeaderConstants.UserEmail].ToString().Should().Be(email);
  }

  [Fact]
  public void GetString_ShouldReturnNull_WhenKeyDoesNotExist()
  {
    // Arrange
    var headers = new MessageHeaders();

    // Act
    var result = headers.GetString("NonExistent");

    // Assert
    result.Should().BeNull();
  }
}