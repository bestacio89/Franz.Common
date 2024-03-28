using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MinervaFramework.Common.SSO;
using MinervaFramework.Common.SSO.Interfaces;
using Moq;
using System.Security.Claims;
using Xunit;

public class SsoManagerTests
{
  [Fact]
  public async Task Login_CallsSsoProvider()
  {
    // Arrange
    var configuration = new Mock<IConfiguration>();
    var ssoProvider = new Mock<ISSoProvider>();
    var userManager = new Mock<UserManager<IdentityUser>>();
    var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object, null, null, null, null, null, null);
    var ssoManager = new GenericSSOManager(ssoProvider.Object, userManager.Object, signInManager.Object);

    // Act
    await ssoManager.Login("test@example.com");

    // Assert
    ssoProvider.Verify(p => p.GetUser("test@example.com"), Times.Once);
  }

  [Fact]
  public async Task Login_ReturnsFalse_IfUserIsNull()
  {
    // Arrange
    var configuration = new Mock<IConfiguration>();
    var ssoProvider = new Mock<ISSoProvider>();
    ssoProvider.Setup(p => p.GetUser("test@example.com")).Returns(new IdentityUser { UserName = "test@example.com", Email = "test@example.com" });
    var userManager = new Mock<UserManager<IdentityUser>>();
    var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object, null, null, null, null, null, null);
    var ssoManager = new GenericSSOManager(ssoProvider.Object, userManager.Object, signInManager.Object);

    // Act
    var result = await ssoManager.Login("test@example.com");

    // Assert
    Assert.False(result);
  }

  [Fact]
  public async Task Login_ReturnsTrue_IfUserIsNotNull()
  {
    // Arrange
    var configuration = new Mock<IConfiguration>();
    var ssoProvider = new Mock<ISSoProvider>();
    ssoProvider.Setup(p => p.GetUser("test@example.com")).Returns(new IdentityUser { UserName = "test@example.com", Email = "test@example.com" });
    var userManager = new Mock<UserManager<IdentityUser>>();
    var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object, null, null, null, null, null, null);
    signInManager.Setup(s => s.SignInAsync(It.IsAny<IdentityUser>(), It.IsAny<bool>(), It.IsAny<string>()))
    .Returns(Task.CompletedTask);
    var ssoManager = new GenericSSOManager(ssoProvider.Object, userManager.Object, signInManager.Object);

    // Act
    var result = await ssoManager.Login("test@example.com");

    // Assert
    Assert.True(result);
  }

  [Fact]
  public async Task Logout_CallsSignInManager()
  {
    // Arrange
    var configuration = new Mock<IConfiguration>();
    var ssoProvider = new Mock<ISSoProvider>();
    var userManager = new Mock<UserManager<IdentityUser>>();
    var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object, null, null, null, null, null, null);
    var ssoManager = new GenericSSOManager(ssoProvider.Object, userManager.Object, signInManager.Object);

    // Act
    await ssoManager.Logout();

    // Assert
    signInManager.Verify(s => s.SignOutAsync(), Times.Once);
  }
}