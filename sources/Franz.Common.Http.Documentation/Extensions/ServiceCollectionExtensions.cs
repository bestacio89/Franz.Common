#nullable enable
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Franz.Common.Http.Documentation.Configuration;
using Franz.Common.Http.Documentation.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
  /// Registers one native OpenAPI document per discovered API version.
  /// Replaces AddSwaggerGen entirely — no Swashbuckle dependency.
  /// </summary>
  public static FranzDocumentationBuilder ConfigureOpenApi(
      this FranzDocumentationBuilder builder)
  {
    // Register the versioned document transformer
    builder.Services.ConfigureOptions<ConfigureVersionedOpenApiOptions>();

    // Defer document registration until versioning is configured
    // so IApiVersionDescriptionProvider is available
    builder.Services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.OpenApi.OpenApiOptions>,
        ConfigureVersionedOpenApiOptions>();

    return builder;
  }

  /// <summary>
  /// Configures API versioning using Asp.Versioning (replaces legacy
  /// Microsoft.AspNetCore.Mvc.Versioning). Registers one OpenAPI document
  /// per version immediately after version discovery.
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

    // Register one OpenAPI document per version
    // Versions are discovered at startup via IApiVersionDescriptionProvider
    builder.Services.AddSingleton<IPostConfigureOptions<
        Microsoft.AspNetCore.OpenApi.OpenApiOptions>,
        RegisterVersionedOpenApiDocuments>();

    return builder;
  }
}

/// <summary>
/// Post-configure step that registers one AddOpenApi() call per
/// discovered API version after versioning has been fully configured.
/// </summary>
internal sealed class RegisterVersionedOpenApiDocuments
    : IPostConfigureOptions<Microsoft.AspNetCore.OpenApi.OpenApiOptions>
{
  private readonly IApiVersionDescriptionProvider _provider;
  private readonly IServiceCollection _services;

  public RegisterVersionedOpenApiDocuments(
      IApiVersionDescriptionProvider provider,
      IServiceCollection services)
  {
    _provider = provider;
    _services = services;
  }

  public void PostConfigure(string? name,
      Microsoft.AspNetCore.OpenApi.OpenApiOptions options)
  {
    foreach (var description in _provider.ApiVersionDescriptions)
      _services.AddOpenApi(description.GroupName);
  }
}