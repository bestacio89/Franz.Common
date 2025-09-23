using Franz.Common.Business.Helpers;
using Franz.Common.Business.Properties;
using Franz.Common.Errors;
using Franz.Common.Mediator;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Franz.Common.Business.Extensions;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Registers Business + Mediator automatically.
  /// </summary>
  public static IServiceCollection AddBusinessWithMediator(
      this IServiceCollection services,
      Assembly entryAssembly,
      Action<FranzMediatorOptions>? configure = null)
  {
    var productName = string.Join(".", entryAssembly!.GetName().Name!.Split(".").Take(2));
    var applicationAssemblyName = $"{productName}.Application";

    var applicationAssembly = SearchApplicationAssemblyInCurrentAppDomain(applicationAssemblyName)
        ?? throw new TechnicalException(Resources.ApplicationDependencyNotExistsException);

    // 🔹 Forward into Franz.Common.Mediator
    services.AddFranzMediator(new[] { applicationAssembly }, configure);

    // 🔹 Collect handlers (domain + application layer)
    HandlerCollector.CollectHandlers(services, applicationAssembly);

    // 🔹 Log success (if logger is present in DI)
    using var provider = services.BuildServiceProvider();
    var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("Franz.BusinessBootstrap");
    logger?.LogInformation("✅ Franz.Business bootstrapped with {AppAssembly}", applicationAssembly.FullName);

    return services;
  }

  /// <summary>
  /// Registers Business + Mediator, but continues without throwing if Application assembly is missing.
  /// </summary>
  public static IServiceCollection TryAddBusinessWithMediator(
      this IServiceCollection services,
      Assembly entryAssembly,
      Action<FranzMediatorOptions>? configure = null)
  {
    var productName = string.Join(".", entryAssembly!.GetName().Name!.Split(".").Take(2));
    var applicationAssemblyName = $"{productName}.Application";

    var applicationAssembly = SearchApplicationAssemblyInCurrentAppDomain(applicationAssemblyName);
    if (applicationAssembly == null)
    {
      using var provider = services.BuildServiceProvider();
      var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("Franz.BusinessBootstrap");
      logger?.LogWarning("⚠️ No Application assembly found for {Name}, Business layer not registered.", applicationAssemblyName);
      return services;
    }

    return services.AddBusinessWithMediator(entryAssembly, configure);
  }

  /// <summary>
  /// Registers Business + Mediator + all FranzPlatform resilience/logging pipelines.
  /// Useful when you want the full stack wired in.
  /// </summary>
  public static IServiceCollection AddFranzPlatform(
      this IServiceCollection services,
      Assembly entryAssembly,
      Action<FranzMediatorOptions>? configure = null)
  {
    services.AddBusinessWithMediator(entryAssembly, configure);

    // 🔹 Add Serilog-aware logging pipeline
    services.AddFranzSerilogLoggingPipeline();

    // 🔹 Add Resilience pipelines (using default policy names)
    services.AddFranzRetryPipeline();
    services.AddFranzCircuitBreakerPipeline();
    services.AddFranzTimeoutPipeline();
    services.AddFranzBulkheadPipeline();

    return services;
  }

  private static Assembly? SearchApplicationAssemblyInCurrentAppDomain(string applicationAssemblyName)
  {
    return AppDomain.CurrentDomain
        .GetAssemblies()
        .FirstOrDefault(assembly =>
        {
          var name = assembly.GetName().Name
                     ?? throw new TechnicalException("Assembly without a name encountered.");
          return name.Equals(applicationAssemblyName, StringComparison.InvariantCultureIgnoreCase);
        });
  }

}
