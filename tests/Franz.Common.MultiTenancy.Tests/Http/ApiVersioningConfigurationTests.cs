using Asp.Versioning;
using FluentAssertions;
using Franz.Common.Http.Documentation.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    var request = new DefaultHttpContext().Request;
    request.Headers.Accept = "application/json; x-api-version=1.0";
    request.Headers["x-api-version"] = "1.0";

    reader.Read(request).Should().BeEmpty();
  }
}
