using Franz.Common.Reflection.Extensions;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IServiceCollection AddCommonArchitecture(this IServiceCollection services, IConfiguration configuration, Assembly? assembly = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    assembly ??= Assembly.GetCallingAssembly();

    services
      .AddCallingAssembly(assembly)
      .AddMediator(assembly)
      .AddDependencies()
      .AddAutoMapper();

    return services;
  }
}
