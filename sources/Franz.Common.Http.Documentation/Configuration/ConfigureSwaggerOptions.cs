#nullable enable
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

using System.Reflection;

namespace Franz.Common.Http.Documentation.Configuration;

/// <summary>
/// Configures one OpenAPI document per API version using the native
/// Microsoft.AspNetCore.OpenApi pipeline.
/// Replaces the Swashbuckle ConfigureSwaggerOptions pattern entirely.
/// </summary>
public sealed class ConfigureVersionedOpenApiOptions : IConfigureOptions<OpenApiOptions>
{
  private readonly IApiVersionDescriptionProvider _provider;

  public ConfigureVersionedOpenApiOptions(IApiVersionDescriptionProvider provider)
  {
    _provider = provider;
  }

  public void Configure(OpenApiOptions options)
  {
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
      var apiName = Assembly.GetEntryAssembly()!.GetName().Name;

      var versionDescription = _provider.ApiVersionDescriptions
          .FirstOrDefault(d => d.GroupName == context.DocumentName);

      document.Info = new OpenApiInfo
      {
        Title = apiName,
        Version = versionDescription?.ApiVersion.ToString() ?? context.DocumentName,
        Description = versionDescription?.IsDeprecated == true
              ? "This API version has been deprecated. Please use one of the new APIs available from the explorer."
              : null
      };

      return Task.CompletedTask;
    });
  }
}