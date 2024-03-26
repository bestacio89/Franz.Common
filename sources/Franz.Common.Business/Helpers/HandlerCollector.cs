using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Business.Helpers;
public class HandlerCollector
{
  public static void CollectHandlers(IServiceCollection services, Assembly assembly)
  {
    var handlerInterfaceType = typeof(IRequestHandler<,>);

    foreach (var type in assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition))
    {
      var interfaces = type.GetInterfaces();
      var handlerInterface = interfaces.FirstOrDefault(i => i.IsGenericType &&
          i.GetGenericTypeDefinition() == handlerInterfaceType);

      if (handlerInterface != null)
      {
        var requestType = handlerInterface.GetGenericArguments()[0];
        var responseType = handlerInterface.GetGenericArguments()[1];

        services.AddTransient(handlerInterface, type);
      }
    }
  }
}
