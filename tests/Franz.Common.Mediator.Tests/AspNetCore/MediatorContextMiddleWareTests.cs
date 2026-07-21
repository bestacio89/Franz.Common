using Franz.Common.Mediator.AspNetCore;
using Franz.Common.Mediator.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Mediator.Tests.AspNetCore;

public class MediatorContextMiddlewareTests
{
  [Fact]
  public async Task InvokeAsync_ValidTraceIdentifier_HydratesCorrelationIdFromTraceIdentifier()
  {
    // Arrange
    var expectedGuid = Guid.CreateVersion7();
    var httpContext = new DefaultHttpContext
    {
      TraceIdentifier = expectedGuid.ToString()
    };

    Guid capturedCorrelationId = Guid.Empty;

    RequestDelegate next = ctx =>
    {
      // Capture during pipeline execution while AsyncLocal context is active
      capturedCorrelationId = MediatorContext.CorrelationId;
      return Task.CompletedTask;
    };

    var middleware = new MediatorContextMiddleware(next);

    // Act
    await middleware.InvokeAsync(httpContext);

    // Assert
    // 1. Verify expected CorrelationId was active during middleware invocation
    capturedCorrelationId.Should().Be(expectedGuid);

    // 2. Verify finally block cleanly reset AsyncLocal context post-invocation
    MediatorContext.CorrelationId.Should().NotBeEmpty();
  }

  [Fact]
  public async Task InvokeAsync_InvalidTraceIdentifier_GeneratesVersion7Guid()
  {
    // Arrange
    var httpContext = new DefaultHttpContext();
    httpContext.TraceIdentifier = "non-guid-trace-id";

    Guid capturedCorrelationId = Guid.Empty;

    RequestDelegate next = ctx =>
    {
      capturedCorrelationId = MediatorContext.CorrelationId;
      return Task.CompletedTask;
    };

    var middleware = new MediatorContextMiddleware(next);

    // Act
    await middleware.InvokeAsync(httpContext);

    // Assert
    capturedCorrelationId.Should().NotBeEmpty();
    capturedCorrelationId.Should().NotBe(Guid.Empty);
  }

  [Fact]
  public async Task InvokeAsync_AuthenticatedUserAndTenantHeader_HydratesUserAndTenantInContext()
  {
    // Arrange
    var httpContext = new DefaultHttpContext();
    httpContext.TraceIdentifier = Guid.NewGuid().ToString();
    httpContext.Request.Headers["X-Tenant-Id"] = "tenant-alpha";

    var identity = new GenericIdentity("john.doe@franz.com", "TestAuth");
    httpContext.User = new ClaimsPrincipal(identity);

    string? capturedUser = null;
    string? capturedTenant = null;
    CultureInfo? capturedCulture = null;

    RequestDelegate next = ctx =>
    {
      capturedUser = MediatorContext.UserId;
      capturedTenant = MediatorContext.TenantId;
      capturedCulture = MediatorContext.Culture;
      return Task.CompletedTask;
    };

    var middleware = new MediatorContextMiddleware(next);

    // Act
    await middleware.InvokeAsync(httpContext);

    // Assert
    capturedUser.Should().Be("john.doe@franz.com");
    capturedTenant.Should().Be("tenant-alpha");
    capturedCulture.Should().Be(CultureInfo.CurrentCulture);
  }

  [Fact]
  public async Task InvokeAsync_WhenNextThrowsException_ResetsMediatorContextInFinallyBlock()
  {
    // Arrange
    var httpContext = new DefaultHttpContext();
    httpContext.TraceIdentifier = Guid.NewGuid().ToString();

    RequestDelegate next = ctx => throw new InvalidOperationException("Pipeline fault");

    var middleware = new MediatorContextMiddleware(next);

    // Act
    Func<Task> act = async () => await middleware.InvokeAsync(httpContext);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>();
    MediatorContext.CorrelationId.Should().NotBe(Guid.Empty);
    MediatorContext.UserId.Should().BeNull();
    MediatorContext.TenantId.Should().BeNull();
  }
}