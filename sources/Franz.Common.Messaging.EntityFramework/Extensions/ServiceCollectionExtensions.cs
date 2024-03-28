using Franz.Common.EntityFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddDatabase<TDbContext>(this IServiceCollection services, IHostEnvironment hostEnvironment, IConfiguration configuration)
    where TDbContext : DbContextBase
  {
    services
      .AddMariaDatabase<TDbContext>(configuration)
      .AddGenericRepositories<TDbContext>()
      .AddBehaviors();

    return services;
  }
}
