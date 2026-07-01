#nullable enable
using Franz.Common.Bootstrap.Extensions;
using Franz.Common.Http.Authentication.Extensions;
using Franz.Common.Http.MultiTenancy.Extensions;
using Franz.Common.Http.Headers.Extensions;
using Franz.Common.Http.Identity.Extensions;
using Franz.Common.Http.Documentation.Extensions;
using Franz.Common.Http.Bootstrap.Exceptions;
using Franz.Common.Serialization.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Franz.Common.Http.Extensions;

namespace Franz.Common.Http.Bootstrap.Extensions;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Adds the Franz common HTTP architecture: controllers, auth, routing,
  /// multi-tenancy, serialization, errors, health checks, and global
  /// exception handling.
  ///
  /// FranzGlobalExceptionHandler is registered here automatically — no
  /// per-service exception handler configuration required. Any service
  /// calling AddHttpArchitecture gets production-grade ProblemDetails
  /// exception handling for free, with the Franz exception hierarchy
  /// (TechnicalException → 500, BusinessException → 422,
  /// ValidationException → 400) mapped to correct HTTP status codes.
  /// </summary>
  public static IServiceCollection AddHttpArchitecture(
      this IServiceCollection services,
      IHostEnvironment hostEnvironment,
      IConfiguration configuration,
      Assembly? assembly = null)
  {
    assembly ??= Assembly.GetCallingAssembly();

    services
        .AddCommonArchitecture(configuration, assembly)
        .AddFranzHttp()
        .AddFrenchRouting()
        .AddDefaultCors(configuration)
        .AddForwardedHeaders()
        .AddSerialization()
        .AddErrors();

    services
        .AddFranzAuthentication();

    services
        .AddHttpHeaderContext();

    services
        .AddFranzDocumentation()
        .ConfigureApiVersioning()
        .ConfigureOpenApi();

    services
        .AddHttpIdentityContext()
        .AddFranzMultiTenancy()
        .AddSerializers()
        .AddHealthChecks();

    // Global exception handler — terminal handler for all unhandled exceptions.
    // Maps Franz + standard .NET exceptions to RFC 7807 ProblemDetails responses.
    // Environment-aware: full detail in Development, sanitized reference in Production.
    services
        .AddExceptionHandler<FranzGlobalExceptionHandler>()
        .AddProblemDetails();

    // Note: Refit support is now provided in Franz.Common.Http.Refit
    // via AddFranzRefit<TClient>. Consumers must opt-in explicitly.

    return services;
  }
}