using Franz.Common.Headers;

namespace Microsoft.Extensions.DependencyInjection;
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
