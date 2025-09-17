using Franz.Common.DependencyInjection.Extensions;
using Franz.Common.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Headers.Extensions;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddHeaderPropagation(this IServiceCollection services, HeaderPropagationSetting headerPropagationSetting)
  {
    services
      .AddNoDuplicateSingleton<IHeaderPropagationRegistrer, HeaderPropagationRegister>()
      .AddSingleton<IHeaderPropagationSetting>(headerPropagationSetting);

    return services;
  }
}
