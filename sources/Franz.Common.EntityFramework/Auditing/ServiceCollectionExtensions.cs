using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.EntityFramework.Auditing;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddFranzAuditing(this IServiceCollection services)
  {
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    return services;
  }
}