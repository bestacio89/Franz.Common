using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder; // only for Swagger
using System.Reflection;

namespace Franz.Common.Http.Bootstrap.Extensions;

public static class ApplicationBuilderExtensions
{
  public static IApplicationBuilder UseDocumentation(this IApplicationBuilder app)
  {
    var apiVersionProvider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
      var apiName = Assembly.GetEntryAssembly()!.GetName().Name;

      foreach (var version in apiVersionProvider.ApiVersionDescriptions)
      {
        options.SwaggerEndpoint(
          $"/swagger/{version.GroupName}/swagger.json",
          $"{apiName} {version.GroupName.ToUpperInvariant()}"
        );
      }
    });

    return app;
  }
}
