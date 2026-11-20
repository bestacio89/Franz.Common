using Asp.Versioning;
using FluentAssertions;
using Franz.Common.Http.Documentation.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class ApiVersioningConfigurationTests
{
  [Fact]
  public void ConfigureApiVersioning_UsesUrlSegmentReaderExclusively()
  {
    var services = new ServiceCollection();
    services
        .AddFranzDocumentation()
        .ConfigureApiVersioning();

    using var provider = services.BuildServiceProvider();
    var options = provider
        .GetRequiredService<IOptions<ApiVersioningOptions>>()
        .Value;

    options.ApiVersionReader.Should().BeOfType<UrlSegmentApiVersionReader>();
    options.DefaultApiVersion.Should().Be(new ApiVersion(1, 0));
    options.AssumeDefaultVersionWhenUnspecified.Should().BeTrue();
    options.ReportApiVersions.Should().BeTrue();
  }

  [Fact]
  public void ConfigureApiVersioning_DoesNotReadVersionFromAcceptOrCustomHeader()
  {
    var services = new ServiceCollection();
    services
        .AddFranzDocumentation()
        .ConfigureApiVersioning();

    using var provider = services.BuildServiceProvider();
    var reader = provider
        .GetRequiredService<IOptions<ApiVersioningOptions>>()
        .Value
        .ApiVersionReader;

    var context = new DefaultHttpContext
    {
      RequestServices = provider
    };
    context.Request.Headers["Accept"] = "application/json; x-api-version=1.0";
    context.Request.Headers["x-api-version"] = "1.0";

    reader.Read(context.Request).Should().BeEmpty();
  }

  [Fact]
  public async Task VersionedEndpoint_ReturnsJsonContentTypeWithoutApiVersionParameter()
  {
    var hostBuilder = new WebHostBuilder()
        .ConfigureServices(services =>
        {
          services
              .AddFranzDocumentation()
              .ConfigureApiVersioning();

          services
              .AddControllers()
              .AddApplicationPart(typeof(ApiVersioningProbeController).Assembly);
        })
        .Configure(application =>
        {
          application.UseRouting();
          application.UseEndpoints(endpoints => endpoints.MapControllers());
        });

    using var server = new TestServer(hostBuilder);
    using var client = server.CreateClient();
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));

    using var response = await client.GetAsync("/api/v1/versioning-probe");

    response.EnsureSuccessStatusCode();

    var contentType = response.Content.Headers.ContentType;
    contentType.Should().NotBeNull();
    contentType!.MediaType.Should().Be("application/json");
    contentType.Parameters.Should().NotContain(parameter =>
        string.Equals(parameter.Name, "x-api-version", StringComparison.OrdinalIgnoreCase));
  }
}
