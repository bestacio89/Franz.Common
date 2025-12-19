#nullable enable
using Franz.Common.Annotations;
using Franz.Common.Reflection;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Reflection;

namespace Franz.Common.Messaging.Kafka;

public static class TopicNamer
{
  private const string TopicSuffixName = "-in";
  private const string DeadLetterTopicSuffixName = "-in-dlt";

  public static string GetTopicName(IAssembly assembly)
  {
    if (assembly is null)
      throw new ArgumentNullException(nameof(assembly));

    var reflectionAssembly = assembly.Assembly;

    var controllerType = reflectionAssembly
      .GetTypes()
      .FirstOrDefault(t =>
        t.IsClass &&
        !t.IsAbstract &&
        typeof(ControllerBase).IsAssignableFrom(t));

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
      throw new InvalidOperationException($"Assembly {reflectionAssembly.FullName} has no valid name");

    return GetServiceName(assemblyName) + TopicSuffixName;
  }

  public static string GetDeadLetterTopicName(IAssembly assembly)
  {
    if (assembly is null)
      throw new ArgumentNullException(nameof(assembly));

    var reflectionAssembly = assembly.Assembly;

    var controllerType = reflectionAssembly
      .GetTypes()
      .FirstOrDefault(t =>
        t.IsClass &&
        !t.IsAbstract &&
        typeof(ControllerBase).IsAssignableFrom(t));

    if (controllerType is not null)
    {
      var attribute = controllerType.GetCustomAttribute<RequiredKafkaTopicAttribute>();
      if (attribute is not null && !string.IsNullOrWhiteSpace(attribute.DeadLetterTopic))
        return attribute.DeadLetterTopic;
    }

    return GetTopicName(assembly) + DeadLetterTopicSuffixName;
  }

  private static string GetEntityNameFromController(Type controllerType)
    => controllerType.Name.Replace("Controller", "", StringComparison.Ordinal);

  private static string GetServiceName(string assemblyName)
  {
    var parts = assemblyName.Split('.', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length >= 2)
      return parts[1].ToLowerInvariant();

    throw new InvalidOperationException(
      $"Unable to extract service name from assembly name: {assemblyName}");
  }
}
