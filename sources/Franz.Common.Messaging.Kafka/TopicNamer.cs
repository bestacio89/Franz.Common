#nullable enable
using Franz.Common.Annotations;
using Franz.Common.Reflection;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Franz.Common.Messaging.Kafka;

public static class TopicNamer
{
  private const string TopicSuffixName = "-in";
  private const string DeadLetterTopicSuffixName = "-in-dlt";

  public static string GetTopicName(IAssembly assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);

    var reflectionAssembly = assembly.Assembly;

    // Senior Note: Scan for a controller that explicitly has our attribute first.
    // This prevents picking up random controllers in shared assemblies or test projects.
    var controllerType = reflectionAssembly
        .GetTypes()
        .FirstOrDefault(t =>
            t.IsClass &&
            !t.IsAbstract &&
            typeof(ControllerBase).IsAssignableFrom(t) &&
            t.GetCustomAttribute<RequiredKafkaTopicAttribute>() != null);

    if (controllerType is not null)
    {
      var attribute = controllerType.GetCustomAttribute<RequiredKafkaTopicAttribute>();
      if (attribute is not null)
      {
        if (!string.IsNullOrWhiteSpace(attribute.Format))
        {
          var entityName = GetEntityNameFromController(controllerType);
          return string.Format(attribute.Format, entityName);
        }

        if (!string.IsNullOrWhiteSpace(attribute.Topic))
          return attribute.Topic;
      }
    }

    var assemblyName = reflectionAssembly.GetName().Name;
    if (string.IsNullOrWhiteSpace(assemblyName))
      throw new InvalidOperationException($"Assembly {reflectionAssembly.FullName} has no valid name.");

    return GetServiceName(assemblyName) + TopicSuffixName;
  }

  public static string GetDeadLetterTopicName(IAssembly assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);

    var reflectionAssembly = assembly.Assembly;

    // Try to find the attribute for explicit DLT naming
    var controllerWithAttribute = reflectionAssembly
        .GetTypes()
        .FirstOrDefault(t =>
            t.IsClass &&
            typeof(ControllerBase).IsAssignableFrom(t) &&
            t.GetCustomAttribute<RequiredKafkaTopicAttribute>()?.DeadLetterTopic != null);

    if (controllerWithAttribute is not null)
    {
      var attribute = controllerWithAttribute.GetCustomAttribute<RequiredKafkaTopicAttribute>();
      return attribute!.DeadLetterTopic!;
    }

    // Fallback to standard convention
    return GetTopicName(assembly) + DeadLetterTopicSuffixName;
  }

  private static string GetEntityNameFromController(Type controllerType)
      => controllerType.Name.Replace("Controller", "", StringComparison.Ordinal);

  private static string GetServiceName(string assemblyName)
  {
    if (assemblyName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
        assemblyName.Contains("vsts", StringComparison.OrdinalIgnoreCase))
    {
      return "franz-test";
    }

    var parts = assemblyName.Split('.');
    if (parts.Length >= 2)
    {
      return parts[1].ToLowerInvariant();
    }

    return assemblyName.ToLowerInvariant();
  }
}