using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Franz.Common.Http.Documentation.Configuration;

public class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
  private readonly IApiVersionDescriptionProvider apiVersionDescriptionProvider;

  public ConfigureSwaggerOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
  {
    this.apiVersionDescriptionProvider = apiVersionDescriptionProvider;
  }

  public void Configure(SwaggerGenOptions options)
  {
    foreach (var apiVersionDescriptions in apiVersionDescriptionProvider.ApiVersionDescriptions)
      options.SwaggerDoc(apiVersionDescriptions.GroupName, CreateVersionInfo(apiVersionDescriptions));
  }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
  public void Configure(string name, SwaggerGenOptions options)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
  {
  }

  private OpenApiInfo CreateVersionInfo(ApiVersionDescription desc)
  {
    var apiName = Assembly.GetEntryAssembly()!.GetName().Name;

    var result = new OpenApiInfo
    {
      Title = apiName,
      Version = desc.ApiVersion.ToString()
    };

    if (desc.IsDeprecated)
      result.Description += " This API version has been deprecated. Please use one of the new APIs available from the explorer.";

    return result;
  }
}
