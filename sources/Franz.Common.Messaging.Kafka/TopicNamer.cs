using Franz.Common.Annotations;
using Franz.Common.Business.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

public static class TopicNamer
{
  private const string TopicSuffixName = "-in";
  private const string DeadLetterTopicSuffixName = "-in-dlt";

  public static string GetTopicName(Assembly assembly)
  {
    var controllerType = assembly.GetTypes()
        .FirstOrDefault(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

    if (controllerType != null)
    {
      var attribute = controllerType.GetCustomAttribute<RequiredKafkaTopicAttribute>();
      if (attribute != null)
      {
        if (!string.IsNullOrEmpty(attribute.Format))
        {
          var entityName = GetEntityNameFromController(controllerType);
          return string.Format(attribute.Format, entityName);
        }
        else
        {
          return attribute.Topic;
        }
      }
    }

    // Null-safe assembly name check
    if (assembly.GetName().Name is string assemblyName)
    {
      return GetServiceName(assemblyName) + TopicSuffixName;
    }

    throw new InvalidOperationException($"Assembly {assembly.FullName} has no valid name");
  }

  private static string GetEntityNameFromController(Type controllerType)
  {
    return controllerType.Name.Replace("Controller", "");
  }

  private static string GetServiceName(string assemblyName)
  {
    var parts = assemblyName.Split('.');
    if (parts.Length >= 2)
    {
      return parts[1].ToLower();
    }

    throw new InvalidOperationException($"Unable to extract service name from assembly name: {assemblyName}");
  }

  public static string GetDeadLetterTopicName(Assembly assembly)
  {
    var controllerType = assembly.GetTypes()
        .FirstOrDefault(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

    if (controllerType != null)
    {
      var attribute = controllerType.GetCustomAttribute<RequiredKafkaTopicAttribute>();
      if (attribute != null && !string.IsNullOrEmpty(attribute.DeadLetterTopic))
      {
        return attribute.DeadLetterTopic;
      }
    }

    return GetTopicName(assembly) + DeadLetterTopicSuffixName;
  }
}
