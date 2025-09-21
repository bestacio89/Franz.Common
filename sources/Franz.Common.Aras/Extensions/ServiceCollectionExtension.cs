using Franz.Common.Aras.Abstractions.Contexts.Contracts;
using Franz.Common.Aras.Extensions.Options;
using Franz.Common.Aras.Innovator.Contexts;
using Franz.Common.Aras.Mappings.Contracts.Factories;
using Franz.Common.Aras.Mappings.Factories;
using Franz.Common.Aras.Mappings.Implementations.Factories;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http;
using System;

namespace Franz.Common.Aras.Innovator.Extensions
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddArasInnovator(
        this IServiceCollection services,
        Action<ArasInnovatorOptions> configureOptions)
    {
      // Bind options
      services.Configure(configureOptions);

      // Register factories (default implementations)
      services.AddSingleton<IArasEntityMapperFactory, ArasEntityMapperFactory>();
      services.AddSingleton<IArasAggregateMapperFactory, ArasAggregateMapperFactory>();

      // Register contexts
      services.AddScoped<IArasEntityContext, ArasInnovatorEntityContext>();
      services.AddScoped<IArasAggregateContext, ArasInnovatorAggregateContext>();

      // Register HttpClient with configured base address & auth header
      services.AddHttpClient<ArasInnovatorEntityContext>()
        .ConfigureHttpClient((sp, client) =>
        {
          var options = sp.GetRequiredService<IOptions<ArasInnovatorOptions>>().Value;
          client.BaseAddress = new Uri(options.BaseUrl);

          if (!string.IsNullOrEmpty(options.AuthToken))
            client.DefaultRequestHeaders.Authorization =
              new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AuthToken);
        });

      services.AddHttpClient<ArasInnovatorAggregateContext>()
        .ConfigureHttpClient((sp, client) =>
        {
          var options = sp.GetRequiredService<IOptions<ArasInnovatorOptions>>().Value;
          client.BaseAddress = new Uri(options.BaseUrl);

          if (!string.IsNullOrEmpty(options.AuthToken))
            client.DefaultRequestHeaders.Authorization =
              new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AuthToken);
        });

      return services;
    }
  }
}
