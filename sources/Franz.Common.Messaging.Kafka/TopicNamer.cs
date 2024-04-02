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
    // Check if the assembly contains a RequiredKafkaTopicAttribute
    
    var controllerType = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t)).First();
    if (controllerType != null)
    {
      // Extract the topic name or format string from the attribute
      var attribute = controllerType.GetCustomAttribute<RequiredKafkaTopicAttribute>();
      if (attribute != null)
      {
        // Use the format string and entity name if present
        if (!string.IsNullOrEmpty(attribute.Format))
        {
          // Extract entity name from controller type (assuming single inheritance)
          var entityName = GetEntityNameFromController(controllerType);
          return string.Format(attribute.Format, entityName);
        }
        else
        {
          // Use the provided topic name if no format string
          return attribute.Topic;
        }
      }
    }

    // Fallback to existing logic (e.g., using assembly name)
    return GetServiceName(assembly.GetName().Name) + TopicSuffixName;
  }

  private static string GetEntityNameFromController(Type controllerType)
  {
    var controllerName = controllerType.Name.Replace("Controller", "");
    return controllerName;
  }

  private static string GetServiceName(string assemblyName)
  {
    // Extract the service name from the second part of the assembly name (e.g., Franz.Common)
    var parts = assemblyName.Split('.');
    if (parts.Length >= 2)
    {
      return parts[1].ToLower();
    }

    // Handle cases where the assembly name doesn't follow the expected format
    throw new InvalidOperationException("Unable to extract service name from assembly name: " + assemblyName);
  }
  public static string GetDeadLetterTopicName(Assembly assembly)
  {
    // Check if the assembly contains a RequiredKafkaTopicAttribute
    var controllerType = assembly.GetTypes().FirstOrDefault(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

    if (controllerType != null)
    {
      var attribute = controllerType.GetCustomAttribute<RequiredKafkaTopicAttribute>();
      if (attribute != null)
      {
        // Use the DeadLetterTopic property from the attribute if present
        if (!string.IsNullOrEmpty(attribute.DeadLetterTopic))
        {
          return attribute.DeadLetterTopic;
        }
      }
    }

    // Fallback to existing logic (appending suffix)
    return GetTopicName(assembly) + DeadLetterTopicSuffixName;
  }
}
