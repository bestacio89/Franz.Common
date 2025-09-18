using Franz.Common.Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Business.Helpers
{
  public static class HandlerCollector
  {
    public static void CollectHandlers(IServiceCollection services, Assembly assembly)
    {
      foreach (var type in assembly.GetTypes()
          .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition))
      {
        var interfaces = type.GetInterfaces();

        // Command handlers
        var commandHandler = interfaces.FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
        if (commandHandler != null)
        {
          services.AddTransient(commandHandler, type);
          continue; // skip to next type
        }

        // Query handlers
        var queryHandler = interfaces.FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
        if (queryHandler != null)
        {
          services.AddTransient(queryHandler, type);
        }
      }
    }
  }
}
