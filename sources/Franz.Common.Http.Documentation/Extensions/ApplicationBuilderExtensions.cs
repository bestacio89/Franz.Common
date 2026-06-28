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
    var versionProvider = app.ApplicationServices
        .GetRequiredService<IApiVersionDescriptionProvider>();

    var apiName = Assembly.GetEntryAssembly()!.GetName().Name ?? "API";

    app.UseEndpoints(endpoints =>
    {
      // One native OpenAPI document per version
      // Available at: /openapi/{groupName}/openapi.json
      foreach (var description in versionProvider.ApiVersionDescriptions)
      {
        endpoints.MapOpenApi(
            $"/openapi/{description.GroupName}/openapi.json");
      }

      // Scalar UI — available at /scalar/{documentName}
      endpoints.MapScalarApiReference(options =>
      {
        options.Title = apiName;

        foreach (var description in versionProvider.ApiVersionDescriptions)
        {
          options.AddDocument(
              documentName: description.GroupName,
              title: $"{apiName} {description.GroupName.ToUpperInvariant()}",
              routePattern: $"/openapi/{description.GroupName}/openapi.json");
        }
      });
    });

    return app;
  }
}