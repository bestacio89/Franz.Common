#nullable enable

using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Http.Errors;
using Franz.Common.Http.Routing;
using Franz.Common.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Franz.Common.Http.Extensions;

public sealed class FranzHttpBuilder
{
  public IServiceCollection Services { get; }

  internal FranzHttpBuilder(IServiceCollection services)
  {
    Services = services;
  }
}

public static class FranzHttpExtensions
{
  private const string CorsValue = "Cors";

  // Single owner of MVC registration
  public static FranzHttpBuilder AddFranzHttp(this IServiceCollection services)
  {
    services.AddControllers();

    return new FranzHttpBuilder(services);
  }

  // CORS
  public static FranzHttpBuilder AddDefaultCors(
      this FranzHttpBuilder builder,
      IConfiguration configuration)
  {
    var originValue = configuration.GetValue<string>(CorsValue);

    builder.Services.AddCors(options =>
    {
      options.AddDefaultPolicy(policy =>
      {
        if (!string.IsNullOrWhiteSpace(originValue))
        {
          var origins = originValue.Split(
              ';',
              StringSplitOptions.RemoveEmptyEntries |
              StringSplitOptions.TrimEntries);

          policy.WithOrigins(origins)
                .SetIsOriginAllowedToAllowWildcardSubdomains()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
          policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
      });
    });

    return builder;
  }

  // Errors
  public static FranzHttpBuilder AddErrors(this FranzHttpBuilder builder)
  {
    builder.Services
        .AddNoDuplicateSingleton<IErrorResponseProvider, ErrorResponseProvider>()
        .AddNoDuplicateScoped<ExceptionFilter>();

    builder.Services.Configure<MvcOptions>(options =>
    {
      options.Filters.AddService<ExceptionFilter>();
    });

    return builder;
  }

  // Serialization
  public static FranzHttpBuilder AddSerialization(this FranzHttpBuilder builder)
  {
    builder.Services.AddSingleton(FranzJson.Default);

    builder.Services.Configure<JsonOptions>(options =>
    {
      options.JsonSerializerOptions.PropertyNamingPolicy =
          JsonNamingPolicy.CamelCase;

      options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;

      foreach (var converter in FranzJson.Default.Converters)
      {
        options.JsonSerializerOptions.Converters.Add(converter);
      }
    });

    return builder;
  }

  // Routing
  public static FranzHttpBuilder AddFrenchRouting(this FranzHttpBuilder builder)
  {
    builder.Services.Configure<MvcOptions>(options =>
    {
      options.Conventions.Add(
          new RouteTokenTransformerConvention(
              new FrenchControllerParameterTransformer()));
    });

    return builder;
  }

  // Forwarded Headers
  public static FranzHttpBuilder AddForwardedHeaders(
      this FranzHttpBuilder builder)
  {
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
      options.ForwardedHeaders =
          ForwardedHeaders.XForwardedFor |
          ForwardedHeaders.XForwardedProto |
          ForwardedHeaders.XForwardedHost;

      options.KnownIPNetworks.Clear();
      options.KnownProxies.Clear();
    });

    return builder;
  }
}