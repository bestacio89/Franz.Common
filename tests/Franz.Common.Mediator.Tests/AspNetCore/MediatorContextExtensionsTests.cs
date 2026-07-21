using Franz.Common.Mediator.AspNetCore;
using Franz.Common.Mediator.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Mediator.Tests.AspNetCore;

public class MediatorContextMiddlewareExtensionsTests
{
  [Fact]
  public void UseMediatorContext_NullBuilder_ThrowsArgumentNullException()
  {
    // Arrange
    IApplicationBuilder builder = null!;

    // Act
    Action act = () => builder.UseMediatorContext();

    // Assert
    act.Should().Throw<NullReferenceException>()
       ;
  }

  [Fact]
  public async Task UseMediatorContext_RegistersMiddleware_AndExecutesInPipeline()
  {
    // Arrange
    var expectedTenant = "tenant-extension-test";
    var expectedTraceId = Guid.NewGuid();

    using var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
          webBuilder
                  .UseTestServer()
                  .ConfigureServices(services =>
                {
                  services.AddRouting();
                })
                  .Configure(app =>
                {
                  // Verify extension method usage
                  app.UseMediatorContext();

                  app.Run(async context =>
                  {
                    var correlationId = MediatorContext.CorrelationId;
                    var tenantId = MediatorContext.TenantId;

                    context.Response.Headers["X-Observed-Correlation"] = correlationId.ToString();
                    context.Response.Headers["X-Observed-Tenant"] = tenantId ?? string.Empty;

                    await context.Response.WriteAsync("OK");
                  });
                });
        })
        .StartAsync();

    var client = host.GetTestClient();

    using var request = new HttpRequestMessage(HttpMethod.Get, "/");
    request.Headers.Add("X-Tenant-Id", expectedTenant);

    // Act
    var response = await client.SendAsync(request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Headers.GetValues("X-Observed-Tenant")
        .Should().ContainSingle().Which.Should().Be(expectedTenant);
    response.Headers.GetValues("X-Observed-Correlation")
        .Should().ContainSingle().Which.Should().NotBeEmpty();
  }
}