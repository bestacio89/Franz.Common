#nullable enable
using FluentAssertions;
using Franz.Common.Errors;
using Franz.Common.Headers;
using Franz.Common.Messaging.Headers;
using Xunit;

namespace Franz.Common.Messaging.Tests.Headers;

public class MessageHeadersTests
{
  [Fact]
  public void TryGetString_WhenKeyExists_ShouldReturnTrueAndValue()
  {
    // FIX: Use collection expression [] to satisfy the string[] requirement
    var headers = new MessageHeaders { { "test-key", ["test-value"] } };

    var success = headers.TryGetString("test-key", out var value);

    success.Should().BeTrue();
    value.Should().Be("test-value");
  }

  [Fact]
  public void TryGetGuid_WhenValidGuid_ShouldReturnTrue()
  {
    var expected = Guid.NewGuid();
    // FIX: Wrap string in an array
    var headers = new MessageHeaders { { "guid-key", [expected.ToString()] } };

    var success = headers.TryGetGuid("guid-key", out var value);

    success.Should().BeTrue();
    value.Should().Be(expected);
  }

  [Fact]
  public void TryGetStringEnumerable_ShouldFilterEmptyAndNulls()
  {
    var headers = new MessageHeaders();
    // FIX: Replaced StringValues with a native string array
    headers.Add("roles", ["Admin", "", " ", null!, "User"]);

    var success = headers.TryGetStringEnumerable("roles", out var values);

    success.Should().BeTrue();
    // Senior Note: Our refactored TryGetStringEnumerable filters out 
    // nulls and whitespace to ensure clean downstream processing.
    values.Should().HaveCount(2).And.ContainInOrder("Admin", "User");
  }



  [Fact]
  public void IdentityHelpers_ShouldSetAndGetCorrectConstants()
  {
    var headers = new MessageHeaders();
    var userId = Guid.NewGuid();
    var roles = new[] { "Manager", "Editor" };

    // The internal Setters already handle the array wrapping logic
    headers.SetIdentityId(userId);
    headers.SetIdentityRoles(roles);

    headers.TryGetIdentityId(out var actualId).Should().BeTrue();
    actualId.Should().Be(userId);

    headers.TryGetIdentityRoles(out var actualRoles).Should().BeTrue();
    actualRoles.Should().BeEquivalentTo(roles);
  }
}