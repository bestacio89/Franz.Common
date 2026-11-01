#nullable enable

using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Http.Errors;
using Franz.Common.Http.Routing;
using Franz.Common.Http.Errors.Strategy;
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
        ArgumentNullException.ThrowIfNull(services);
        services.AddControllers();
        return new FranzHttpBuilder(services);
    }

    // CORS uses exact, explicitly configured origins. An absent configuration
    // deliberately produces a same-origin-only policy rather than a permissive default.
    public static FranzHttpBuilder AddDefaultCors(
        this FranzHttpBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var origins = ParseCorsOrigins(configuration.GetValue<string>(CorsValue));

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod();

                if (origins.Length > 0)
                    policy.WithOrigins(origins).AllowCredentials();
            });
        });

        return builder;
    }

    private static string[] ParseCorsOrigins(string? originValue)
    {
        if (string.IsNullOrWhiteSpace(originValue))
            return [];

        var values = originValue.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var origins = new List<string>(values.Length);
        foreach (var value in values)
        {
            var origin = value.TrimEnd('/');
            if (origin.Contains('*') ||
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                uri.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"CORS origin '{value}' is invalid. Configure an exact HTTP or HTTPS origin without a path, wildcard, query, or credentials.");
            }

            origins.Add(uri.GetLeftPart(UriPartial.Authority));
        }

        return origins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    // Errors
    public static FranzHttpBuilder AddErrors(this FranzHttpBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddNoDuplicateSingleton<IErrorResponseProvider, ErrorResponseProvider>()
            .AddNoDuplicateScoped<IErrorResponseStrategy, DefaultErrorResponseStrategy>()
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
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton(FranzJson.Default);
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;

            foreach (var converter in FranzJson.Default.Converters)
                options.JsonSerializerOptions.Converters.Add(converter);
        });

        return builder;
    }

    // Routing
    public static FranzHttpBuilder AddFrenchRouting(this FranzHttpBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<MvcOptions>(options =>
        {
            options.Conventions.Add(
                new RouteTokenTransformerConvention(
                    new FrenchControllerParameterTransformer()));
        });

        return builder;
    }

    // Forwarded headers
    // The default overload trusts only ASP.NET Core's built-in loopback proxies.
    // Deployments must explicitly add their reverse proxies/networks.
    public static FranzHttpBuilder AddForwardedHeaders(this FranzHttpBuilder builder)
        => builder.AddForwardedHeaders(configure: null);

    public static FranzHttpBuilder AddForwardedHeaders(
        this FranzHttpBuilder builder,
        Action<ForwardedHeadersOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            configure?.Invoke(options);
        });

        return builder;
    }
}
