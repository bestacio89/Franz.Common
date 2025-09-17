using Franz.Common.Http.Errors;
using Franz.Common.Http.Routing;
using Franz.Common.Serialization.Converters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Franz.Common.DependencyInjection.Extensions;
namespace Franz.Common.Http;

public static class ServiceCollectionExtensions
{
  private const string CorsValue = "Cors";

  public static IServiceCollection AddHttpControllers(this IServiceCollection services)
  {
    services
      .AddControllers();

    return services;
  }

  public static IServiceCollection AddHttpAuthentication(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddAuthorization();

    return services;
  }

  public static IServiceCollection AddDefaultCors(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddCors(options =>
      {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        var origin = configuration.GetValue<string?>(CorsValue) ?? string.Empty;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        options.AddDefaultPolicy(policy =>
        {
          policy.AddPolicy(origin);
        });
      });

    return services;
  }

  public static CorsPolicyBuilder AddPolicy(this CorsPolicyBuilder corsPolicyBuilder, string origin)
  {
    var origins = origin.Split(";");

    corsPolicyBuilder
      .WithOrigins(origins.ToArray())
      .SetIsOriginAllowedToAllowWildcardSubdomains()
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();

    return corsPolicyBuilder;
  }

  public static IServiceCollection AddFrenchRouting(this IServiceCollection services)
  {
    services
      .AddControllers(options =>
      {
        options.Conventions.Add(
            new RouteTokenTransformerConvention(
                new FrenchControllerParameterTransformer()));
      });

    return services;
  }

  public static IServiceCollection AddForwardedHeaders(this IServiceCollection services)
  {
    services.Configure<ForwardedHeadersOptions>(options =>
    {
      options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
      options.KnownNetworks.Clear();
      options.KnownProxies.Clear();
    });

    return services;
  }

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

  public static IServiceCollection AddHttpSerialization(this IServiceCollection services)
  {
    services
      .AddControllers()
      .AddNewtonsoftJson(options =>
      {
        options.SerializerSettings.Converters = new List<JsonConverter> { new DateTimeJsonConverter(), new DateTimeOffsetJsonConverter(), new EnumerationJsonConverter() };
      });

    return services;
  }
}
