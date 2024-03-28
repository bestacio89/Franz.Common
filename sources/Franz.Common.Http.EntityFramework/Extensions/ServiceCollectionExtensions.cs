using Franz.Common.EntityFramework;
using Franz.Common.Http.EntityFramework.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddDatabase<TDbContext>(this IServiceCollection services, IHostEnvironment hostEnvironment, IConfiguration configuration)
    where TDbContext : DbContextBase
  {
    services
      .AddDatabaseTransactionPerHttpCall()
      .AddMariaDatabase<TDbContext>(configuration)
      .AddGenericRepositories<TDbContext>()
      .AddBehaviors();

    return services;
  }

  public static IServiceCollection AddDatabaseTransactionPerHttpCall(this IServiceCollection services)
  {
    services
      .AddNoDuplicateScoped<TransactionFilter>()
      .AddMvc()
      .AddMvcOptions(setup =>
      {
        setup.Filters.AddService<TransactionFilter>();
      });

    return services;
  }
}
