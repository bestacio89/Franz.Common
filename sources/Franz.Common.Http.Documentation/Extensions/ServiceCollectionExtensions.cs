#nullable enable
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Franz.Common.Http.Documentation.Configuration;
using Franz.Common.Http.Documentation.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

using System.Reflection;

namespace Franz.Common.Http.Documentation.Extensions;

public sealed class FranzDocumentationBuilder
{
  internal IServiceCollection Services { get; }

  internal FranzDocumentationBuilder(IServiceCollection services)
  {
    Services = services;
  }
}

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Entry point to add Franz documentation pipeline.
  /// </summary>
  public static FranzDocumentationBuilder AddFranzDocumentation(
      this IServiceCollection services)
  {
    return new FranzDocumentationBuilder(services);
  }

  /// <summary>
  /// Configures API versioning using Asp.Versioning — replaces legacy
  /// Microsoft.AspNetCore.Mvc.Versioning. Must be called before ConfigureOpenApi.
  /// </summary>
  public static FranzDocumentationBuilder ConfigureApiVersioning(
      this FranzDocumentationBuilder builder)
  {
    builder.Services.AddControllers(options =>
    {
      options.UseGeneralRoutePrefix("api/v{version:apiVersion}");
    });

    builder.Services
        .AddApiVersioning(opt =>
        {
          opt.DefaultApiVersion = new ApiVersion(1, 0);
          opt.AssumeDefaultVersionWhenUnspecified = true;
          opt.ReportApiVersions = true;

          opt.ApiVersionReader = ApiVersionReader.Combine(
                  new UrlSegmentApiVersionReader(),
                  new HeaderApiVersionReader("x-api-version"),
                  new MediaTypeApiVersionReader("x-api-version"));
        })
        .AddApiExplorer(setup =>
        {
          setup.GroupNameFormat = "'v'VVV";
          setup.SubstituteApiVersionInUrl = true;
        });

    return builder;
  }

  /// <summary>
  /// Registers one native OpenAPI document per discovered API version.
  /// Uses a temporary service provider to resolve IApiVersionDescriptionProvider
  /// at registration time — the correct pattern for design-time version discovery.
  /// Replaces AddSwaggerGen entirely — no Swashbuckle dependency.
  /// </summary>
  public static FranzDocumentationBuilder ConfigureOpenApi(
      this FranzDocumentationBuilder builder)
  {
    // Build temporary provider to discover registered versions at registration time
    var tempProvider = builder.Services.BuildServiceProvider();
    var versionProvider = tempProvider
        .GetRequiredService<IApiVersionDescriptionProvider>();

    // Register one OpenAPI document per version with its own document transformer
    foreach (var description in versionProvider.ApiVersionDescriptions)
    {
      var desc = description; // capture for closure

      builder.Services.AddOpenApi(desc.GroupName, options =>
      {
        options.AddDocumentTransformer((document, context, ct) =>
        {
          var apiName = Assembly.GetEntryAssembly()!.GetName().Name;

          document.Info = new OpenApiInfo
          {
            Title = apiName,
            Version = desc.ApiVersion.ToString(),
            Description = desc.IsDeprecated
                  ? "This API version has been deprecated. Please use one of the new APIs available from the explorer."
                  : null
          };

          return Task.CompletedTask;
        });
      });
    }

    return builder;
  }
}