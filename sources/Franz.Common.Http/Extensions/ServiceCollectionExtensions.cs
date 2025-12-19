#nullable enable

using Franz.Common.Http.Errors;
using Franz.Common.Http.Routing;
using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Serialization;
using Franz.Common.Serialization.Converters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Franz.Common.Http;

public static class ServiceCollectionExtensions
{
  private const string CorsValue = "Cors";

  /// <summary>
  /// Registers MVC controllers.
  /// </summary>
  public static IServiceCollection AddHttpControllers(this IServiceCollection services)
  {
    services.AddControllers();
    return services;
  }

  /// <summary>
  /// Registers authentication + authorization defaults.
  /// </summary>
  public static IServiceCollection AddHttpAuthentication(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    services.AddAuthorization();
    return services;
  }

  /// <summary>
  /// Adds CORS configuration based on the `Cors` configuration value.
  /// </summary>
  public static IServiceCollection AddDefaultCors(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    var originValue = configuration.GetValue<string>(CorsValue);

    services.AddCors(options =>
    {
      options.AddDefaultPolicy(policy =>
      {
        if (!string.IsNullOrWhiteSpace(originValue))
          policy.AddPolicyFromOrigins(originValue);
        else
          policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
      });
    });

    return services;
  }

  public static CorsPolicyBuilder AddPolicyFromOrigins(
      this CorsPolicyBuilder builder,
      string origins)
  {
    var originList = origins
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (originList.Length > 0)
    {
      builder
          .WithOrigins(originList)
          .SetIsOriginAllowedToAllowWildcardSubdomains()
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
    }

    return builder;
  }

  /// <summary>
  /// Enables FR-friendly routing transformations.
  /// </summary>
  public static IServiceCollection AddFrenchRouting(this IServiceCollection services)
  {
    services.AddControllers(options =>
    {
      options.Conventions.Add(
          new RouteTokenTransformerConvention(
              new FrenchControllerParameterTransformer()));
    });

    return services;
  }

  /// <summary>
  /// Enables forwarded headers support (reverse proxy awareness).
  /// </summary>
  public static IServiceCollection AddForwardedHeaders(this IServiceCollection services)
  {
    services.Configure<ForwardedHeadersOptions>(options =>
    {
      options.ForwardedHeaders =
          ForwardedHeaders.XForwardedFor |
          ForwardedHeaders.XForwardedProto |
          ForwardedHeaders.XForwardedHost;

      options.KnownNetworks.Clear();
      options.KnownProxies.Clear();
    });

    return services;
  }

  /// <summary>
  /// Adds Franz-standard error formatting.
  /// </summary>
  public static IServiceCollection AddHttpErrors(this IServiceCollection services)
  {
    services
        .AddNoDuplicateSingleton<IErrorResponseProvider, ErrorResponseProvider>()
        .AddNoDuplicateScoped<ExceptionFilter>()
        .AddControllers(options =>
        {
          options.Filters.AddService<ExceptionFilter>();
        });

    return services;
  }

  /// <summary>
  /// Adds System.Text.Json serialization with Franz defaults.
  /// </summary>
  public static IServiceCollection AddHttpSerialization(this IServiceCollection services)
  {
    services.AddSingleton(FranzJson.Default);

    services.AddControllers()
      .AddJsonOptions(options =>
      {
        // Use Franz global JSON config
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;

        foreach (var converter in FranzJson.Default.Converters)
          options.JsonSerializerOptions.Converters.Add(converter);
      });

    return services;
  }
}
