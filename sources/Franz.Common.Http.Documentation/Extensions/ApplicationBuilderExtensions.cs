using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder;
public static class ApplicationBuilderExtensions
{
  public static IApplicationBuilder UseDocumentation(this IApplicationBuilder applicationBuilder)
  {
    var apiVersionDescriptionProvider = applicationBuilder.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
    applicationBuilder.UseSwagger();
    applicationBuilder.UseSwaggerUI(options =>
    {
      var apiName = Assembly.GetEntryAssembly()!.GetName().Name;

      foreach (var apiVersionDescription in apiVersionDescriptionProvider.ApiVersionDescriptions)
      {
        var versionName = apiVersionDescription.GroupName;
        options.SwaggerEndpoint($"/swagger/{versionName}/swagger.json", $"{apiName} {versionName.ToUpperInvariant()}");
      }
    });

    return applicationBuilder;
  }
}
