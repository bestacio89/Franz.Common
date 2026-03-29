using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mapping.Core;
using Franz.Common.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Mapping.Extensions;

public static class MappingExtensions
{
    public static IServiceCollection AddFranzMapping(
        this IServiceCollection services,
        Action<MappingConfiguration>? configure = null,
        params Assembly[] assemblies)
    {
        var config = new MappingConfiguration();
        configure?.Invoke(config);

        Assembly[] scanAssemblies = assemblies.Length > 0 
            ? assemblies 
            : [.. ReflectionHelper.GetCurrentAppDomainAssemblies(ReflectionHelper.GetAssemblyCompanyOrProductPredicate())];

        // Register the Dual-Paradigm fallback FIRST so explicit mappers override it
        services.AddTransient(typeof(IMapper<,>), typeof(BaseAutoMapper<,>));

        foreach (var assembly in scanAssemblies)
        {
            config.AddProfilesFromAssembly(assembly);
            services.AddMappersFromAssembly(assembly);
        }

        services.AddSingleton(config);
        services.AddSingleton<IFranzMapper, FranzMapper>();
        services.AddSingleton<IMappingService, MappingService>();

        return services;
    }

    public static IServiceCollection AddFranzMapping(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddFranzMapping(null, assemblies);
    }

    private static void AddProfilesFromAssembly(this MappingConfiguration config, Assembly assembly)
    {
        var profiles = assembly.GetTypes()
            .Where(t => typeof(IFranzMapProfile).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<IFranzMapProfile>();

        foreach (var profile in profiles)
            profile.Configure(config);
    }

    private static void AddMappersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var mapperTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in mapperTypes)
        {
            var interfaces = type.GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType)
                {
                    var genericDef = iface.GetGenericTypeDefinition();
                    if (genericDef == typeof(IMapper<,>) ||
                        genericDef == typeof(IAsyncMapper<,>) ||
                        genericDef == typeof(IProjection<,>))
                    {
                        services.AddTransient(iface, type);
                    }
                }
            }
        }
    }
}
