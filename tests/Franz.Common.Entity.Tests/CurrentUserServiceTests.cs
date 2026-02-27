using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.EntityFramework.Auditing;
using System.Security.Claims;

namespace Franz.Common.EntityFramework.Tests
{
  // =========================
  // CurrentUserService Tests
  // =========================
  public class CurrentUserServiceTests
  {
    [Fact]
    public void UserId_Returns_UserName_When_HttpContextUserIsNotNull()
    {
      // Arrange
      var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
      var context = new DefaultHttpContext();
      context.User = new ClaimsPrincipal(
          new ClaimsIdentity(
              new[] { new Claim(ClaimTypes.Name, "TestUser") },
              "TestAuth"
          )
      );
      mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(context);

      var service = new CurrentUserService(mockHttpContextAccessor.Object);

      // Act
      var userId = service.UserId;

      // Assert
      Assert.Equal("TestUser", userId);
    }

    [Fact]
    public void UserId_Returns_Null_When_HttpContextIsNull()
    {
      // Arrange
      var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
      mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null);

      var service = new CurrentUserService(mockHttpContextAccessor.Object);

      // Act
      var userId = service.UserId;

      // Assert
      Assert.Null(userId);
    }

    [Fact]
    public void UserId_Returns_Null_When_UserIdentityIsNull()
    {
      // Arrange
      var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
      var context = new DefaultHttpContext();
      context.User = new ClaimsPrincipal(new ClaimsIdentity()); // no Name
      mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(context);

      var service = new CurrentUserService(mockHttpContextAccessor.Object);

      // Act
      var userId = service.UserId;

      // Assert
      Assert.Null(userId);
    }
  }

  // =========================
  // ServiceCollectionExtensions Tests
  // =========================
  public class ServiceCollectionExtensionsTests
  {
    [Fact]
    public void AddFranzAuditing_RegistersRequiredServices()
    {
      // Arrange
      var services = new ServiceCollection();

      // Act
      services.AddFranzAuditing();
      var serviceProvider = services.BuildServiceProvider();

      // Assert
      var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
      Assert.NotNull(httpContextAccessor);

      var currentUserService = serviceProvider.GetService<ICurrentUserService>();
      Assert.NotNull(currentUserService);
      Assert.IsType<CurrentUserService>(currentUserService);
    }
  }
}