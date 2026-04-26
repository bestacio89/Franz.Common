#nullable enable

using Franz.Common.Http.Documentation.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
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
  public static FranzDocumentationBuilder AddFranzDocumentation(this IServiceCollection services)
  {
    return new FranzDocumentationBuilder(services);
  }

  /// <summary>
  /// Configures Swagger (LEGACY MODE SAFE VERSION).
  /// 
  /// IMPORTANT:
  /// - In .NET 10 OpenAPI source generator mode, XML comments must NOT be used.
  /// - This configuration assumes SwaggerGen is the active documentation provider.
  /// </summary>
  public static FranzDocumentationBuilder ConfigureSwagger(this FranzDocumentationBuilder builder)
  {
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
      // -------------------------------
      // XML COMMENTS (SAFE GUARD)
      // -------------------------------
      var entryAssembly = Assembly.GetEntryAssembly();

      if (entryAssembly != null)
      {
        var xmlFilename = $"{entryAssembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);

        // Only include XML comments if file exists AND we are in SwaggerGen mode.
        // This avoids .NET 10 OpenAPI immutable schema conflicts.
        if (File.Exists(xmlPath))
        {
          options.IncludeXmlComments(xmlPath);
        }
      }

      // Framework enum conversion (safe)
      options.ConvertEnumeration();
    });

    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    return builder;
  }

  /// <summary>
  /// Configures API versioning and general route prefix.
  /// </summary>
  public static FranzDocumentationBuilder ConfigureApiVersioning(this FranzDocumentationBuilder builder)
  {
    builder.Services.AddControllers(options =>
    {
      options.UseGeneralRoutePrefix("api/v{version:apiVersion}");
    });

    builder.Services.AddApiVersioning(opt =>
    {
      opt.DefaultApiVersion = new ApiVersion(1, 0);
      opt.AssumeDefaultVersionWhenUnspecified = true;
      opt.ReportApiVersions = true;

      opt.ApiVersionReader = ApiVersionReader.Combine(
          new UrlSegmentApiVersionReader(),
          new HeaderApiVersionReader("x-api-version"),
          new MediaTypeApiVersionReader("x-api-version")
      );
    });

    builder.Services.AddVersionedApiExplorer(setup =>
    {
      setup.GroupNameFormat = "'v'VVV";
      setup.SubstituteApiVersionInUrl = true;
    });

    return builder;
  }
}