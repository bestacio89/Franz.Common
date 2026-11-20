#nullable enable
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Reflection;

namespace Franz.Common.Http.Documentation.Extensions;

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
  /// Configures URL-segment API versioning using Asp.Versioning.
  /// The API version is read exclusively from the route segment.
  /// Must be called before ConfigureOpenApi.
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
          opt.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(setup =>
        {
          setup.GroupNameFormat = "'v'VVV";
          setup.SubstituteApiVersionInUrl = true;
        });

    return builder;
  }

  /// <summary>
  /// Registers the native OpenAPI document for API version 1.
  /// </summary>
  public static FranzDocumentationBuilder ConfigureOpenApi(
     this FranzDocumentationBuilder builder)
  {
    builder.Services.AddOpenApi("v1", options =>
    {
      options.AddDocumentTransformer((document, context, ct) =>
      {
        var apiName = Assembly.GetEntryAssembly()!.GetName().Name;

        document.Info = new OpenApiInfo
        {
          Title = apiName,
          Version = "1.0"
        };

        return Task.CompletedTask;
      });
    });

    return builder;
  }
}
