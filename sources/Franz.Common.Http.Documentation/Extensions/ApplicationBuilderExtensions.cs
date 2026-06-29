#nullable enable
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;
using System.Reflection;

namespace Franz.Common.Http.Bootstrap.Extensions;

public static class ApplicationBuilderExtensions
{
  public static IApplicationBuilder UseDocumentation(this IApplicationBuilder app)
  {
    var apiName = Assembly.GetEntryAssembly()!.GetName().Name ?? "API";

    app.UseEndpoints(endpoints =>
    {
      endpoints.MapOpenApi("/openapi/{documentName}.json");

      endpoints.MapScalarApiReference(options =>
      {
        options.Title = apiName;
        options.AddDocument(
            "v1",
            $"{apiName} V1",
            "/openapi/v1.json");
      });
    });

    return app;
  }
}