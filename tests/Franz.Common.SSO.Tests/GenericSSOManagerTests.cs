using Franz.Common.SSO;
using Franz.Common.SSO.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public sealed class GenericSSOManagerTests
{
  [Fact]
  public async Task Login_returns_false_when_provider_does_not_find_user()
  {
    var provider = new Mock<ISSoProvider>();
    provider.Setup(x => x.GetUser("missing@example.com")).Returns((IdentityUser)null!);
    var userManager = CreateUserManager();
    var signInManager = CreateSignInManager(userManager.Object);
    var sut = new GenericSSOManager(provider.Object, userManager.Object, signInManager.Object);

    var result = await sut.Login("missing@example.com");

    Assert.False(result);
    provider.Verify(x => x.GetUser("missing@example.com"), Times.Once);
    signInManager.Verify(x => x.CheckPasswordSignInAsync(
      It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
  }

  [Fact]
  public async Task Login_signs_in_user_when_password_check_succeeds()
  {
    var user = new IdentityUser { UserName = "user@example.com", Email = "user@example.com" };
    var provider = new Mock<ISSoProvider>();
    provider.Setup(x => x.GetUser(user.Email!)).Returns(user);
    var userManager = CreateUserManager();
    var signInManager = CreateSignInManager(userManager.Object);
    signInManager.Setup(x => x.CheckPasswordSignInAsync(user, string.Empty, false))
      .ReturnsAsync(SignInResult.Success);
    signInManager.Setup(x => x.SignInAsync(user, false)).Returns(Task.CompletedTask);
    var sut = new GenericSSOManager(provider.Object, userManager.Object, signInManager.Object);

    var result = await sut.Login(user.Email!);

    Assert.True(result);
    signInManager.Verify(x => x.SignInAsync(user, false), Times.Once);
  }

  [Fact]
  public async Task Login_returns_false_when_password_check_fails()
  {
    var user = new IdentityUser { UserName = "user@example.com", Email = "user@example.com" };
    var provider = new Mock<ISSoProvider>();
    provider.Setup(x => x.GetUser(user.Email!)).Returns(user);
    var userManager = CreateUserManager();
    var signInManager = CreateSignInManager(userManager.Object);
    signInManager.Setup(x => x.CheckPasswordSignInAsync(user, string.Empty, false))
      .ReturnsAsync(SignInResult.Failed);
    var sut = new GenericSSOManager(provider.Object, userManager.Object, signInManager.Object);

    var result = await sut.Login(user.Email!);

    Assert.False(result);
    signInManager.Verify(x => x.SignInAsync(
      It.IsAny<IdentityUser>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
  }

  [Fact]
  public async Task Logout_signs_out_current_user()
  {
    var provider = new Mock<ISSoProvider>();
    var userManager = CreateUserManager();
    var signInManager = CreateSignInManager(userManager.Object);
    signInManager.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);
    var sut = new GenericSSOManager(provider.Object, userManager.Object, signInManager.Object);

    await sut.Logout();

    signInManager.Verify(x => x.SignOutAsync(), Times.Once);
  }

  private static Mock<UserManager<IdentityUser>> CreateUserManager()
    => new(Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);

  private static Mock<SignInManager<IdentityUser>> CreateSignInManager(
    UserManager<IdentityUser> userManager)
    => new(
      userManager,
      Mock.Of<IHttpContextAccessor>(),
      Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(),
      Options.Create(new IdentityOptions()),
      Mock.Of<ILogger<SignInManager<IdentityUser>>>(),
      Mock.Of<IAuthenticationSchemeProvider>(),
      Mock.Of<IUserConfirmation<IdentityUser>>());
}
