#nullable enable
using Franz.Common.Bootstrap.Extensions;
using Franz.Common.Http;
using Franz.Common.Http.Authentication.Extensions;
using Franz.Common.Http.MultiTenancy.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Franz.Common.Http.Headers.Extensions;
using Franz.Common.Http.Identity.Extensions;
using Franz.Common.Serialization.Extensions;
using Franz.Common.Http.Documentation.Extensions;
using System;
using System.Linq;
using System.Globalization;

namespace Franz.Common.Http.Bootstrap.Extensions;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddHttpArchitecture(this IServiceCollection services, IHostEnvironment hostEnvironment, IConfiguration configuration, Assembly? assembly = null)
  {
    if (assembly == null)
      assembly = Assembly.GetCallingAssembly();

    // Core HTTP wiring (unchanged)
    services
      .AddCommonArchitecture(configuration, assembly)
      .AddHttpControllers()
      .AddFranzAuthentication()
      .AddFrenchRouting()
      .AddDefaultCors(configuration)
      .AddForwardedHeaders()
      .AddHttpHeaderContext()
      .AddHeaderRequiredCapability()
      .AddHttpIdentityContext()
      .AddFranzMultiTenancy()
      .AddSerializers()
      .AddHttpSerialization()
      .AddDocumentation()
      .AddHttpErrors()
      .AddHealthChecks();

    // --- Refit conditional wiring ---
    // config shape expected:
    // "Franz": {
    //   "HttpClients": {
    //     "EnableRefit": true,
    //     "Apis": {
    //       "Weather": {
    //         "InterfaceType": "MyApp.Clients.IWeatherApi, MyApp",
    //         "BaseUrl": "https://api.weather.local",
    //         "Policy": "DefaultHttpRetry"
    //       }
    //     }
    //   }
    // }

    var enableRefit = configuration.GetValue<bool?>("Franz:HttpClients:EnableRefit") ?? false;
    if (enableRefit)
    {
      var apisSection = configuration.GetSection("Franz:HttpClients:Apis");
      if (apisSection.Exists())
      {
        // We'll call AddFranzRefit<TClient>(...) via reflection so this assembly doesn't need compile-time generics.
        // The extension method type should be present at runtime: Franz.Common.Http.Refit.Extensions.FranzRefitServiceCollectionExtensions
        var refitExtType = Type.GetType("Franz.Common.Http.Refit.Extensions.FranzRefitServiceCollectionExtensions, Franz.Common.Http.Refit")
                         ?? Type.GetType("Franz.Common.Refit.Extensions.FranzRefitServiceCollectionExtensions, Franz.Common.Http.Refit"); // fallback attempt

        if (refitExtType == null)
          throw new InvalidOperationException("Franz RefIt extension type 'Franz.Common.Http.Refit.Extensions.FranzRefitServiceCollectionExtensions' not found. Ensure Franz.Common.Http.Refit is referenced by the host project.");

        // find generic AddFranzRefit method (one generic arg)
        var addFranzRefitMethod = refitExtType
          .GetMethods(BindingFlags.Public | BindingFlags.Static)
          .FirstOrDefault(mi => mi.Name == "AddFranzRefit" && mi.IsGenericMethodDefinition && mi.GetGenericArguments().Length == 1);

        if (addFranzRefitMethod == null)
          throw new InvalidOperationException("AddFranzRefit<TClient> method not found on FranzRefitServiceCollectionExtensions. Confirm the method signature.");

        // iterate APIs
        foreach (var api in apisSection.GetChildren())
        {
          var apiName = api.Key;
          var baseUrl = api["BaseUrl"]?.Trim();
          var policy = api["Policy"]?.Trim();

          if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException($"Refit API '{apiName}' must define a BaseUrl (Franz:HttpClients:Apis:{apiName}:BaseUrl).");

          // 1) Prefer explicit InterfaceType from config (assembly-qualified)
          var interfaceTypeName = api["InterfaceType"];
          Type? interfaceType = null;

          if (!string.IsNullOrWhiteSpace(interfaceTypeName))
          {
            interfaceType = Type.GetType(interfaceTypeName!, throwOnError: false, ignoreCase: true);
          }

          // 2) Fallback: try to locate interface in provided assembly by simple name match
          if (interfaceType == null && assembly != null)
          {
            // match by full name, then by type name
            interfaceType = assembly.GetTypes().FirstOrDefault(t =>
              t.IsInterface &&
              (string.Equals(t.FullName, apiName, StringComparison.InvariantCultureIgnoreCase)
               || string.Equals(t.Name, apiName, StringComparison.InvariantCultureIgnoreCase)));
          }

          // 3) Last attempt: scan all loaded assemblies if not found in provided assembly
          if (interfaceType == null)
          {
            interfaceType = AppDomain.CurrentDomain.GetAssemblies()
              .SelectMany(a =>
              {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
              })
              .FirstOrDefault(t => t.IsInterface &&
                    (string.Equals(t.FullName, apiName, StringComparison.InvariantCultureIgnoreCase)
                     || string.Equals(t.Name, apiName, StringComparison.InvariantCultureIgnoreCase)));
          }

          if (interfaceType == null)
          {
            throw new InvalidOperationException(
              $"Could not resolve interface for Refit client '{apiName}'. Provide an assembly-qualified InterfaceType in config (e.g. 'MyApp.Clients.IWeatherApi, MyApp') or ensure the interface type is discoverable in the provided assembly.");
          }

          // Now invoke AddFranzRefit<TClient>(IServiceCollection, string name, string baseUrl, string? policyName = null, Action<RefitSettings>? configureRefitSettings = null, Action<RefitClientOptions>? configureOptions = null)
          var genericMethod = addFranzRefitMethod.MakeGenericMethod(interfaceType);

          // prepare args: (IServiceCollection services, string name, string baseUrl, string? policyName, Action<RefitSettings>?, Action<RefitClientOptions>?)
          var args = new object?[] { services, apiName, baseUrl, string.IsNullOrWhiteSpace(policy) ? null : policy, null, null };

          // invoke and reassign returned IServiceCollection (AddFranzRefit returns IServiceCollection)
          var result = genericMethod.Invoke(null, args);
          if (result is IServiceCollection sc)
          {
            services = sc;
          }
          else
          {
            throw new InvalidOperationException($"AddFranzRefit invocation for '{apiName}' did not return IServiceCollection.");
          }
        } // foreach api
      } // apis section exists
    } // enableRefit

    return services;
  }
}
