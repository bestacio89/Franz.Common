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
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        return services;
    }

    /// <summary>
    /// Internal helper to attach a CORS policy to a set of origins.
    /// Accepts semicolon-separated origin list.
    /// </summary>
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
    /// Enables automatic FR-friendly routing transformations.
    /// Example: /Mes-Utilisateurs instead of /MesUtilisateurs.
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
    /// Adds date/time + enumeration JSON converters.
    /// </summary>
    public static IServiceCollection AddHttpSerialization(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters = new List<JsonConverter>
                {
                    new DateTimeJsonConverter(),
                    new DateTimeOffsetJsonConverter(),
                    new EnumerationJsonConverter()
                };
            });

        return services;
    }
}
