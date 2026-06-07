#nullable enable
using Franz.Common.Annotations;
using Franz.Common.Reflection;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Franz.Common.Messaging.Kafka;

public static class TopicNamer
{
  private const string TopicSuffixName = "-in";
  private const string DeadLetterTopicSuffixName = "-in-dlt";

  // =========================================================
  // SYSTEM MODE — assembly-based (one topic per service)
  // =========================================================

  public static string GetTopicName(IAssembly assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);

    var reflectionAssembly = assembly.Assembly;

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
      throw new InvalidOperationException(
          $"Assembly {reflectionAssembly.FullName} has no valid name.");

    return GetServiceName(assemblyName) + TopicSuffixName;
  }

  public static string GetDeadLetterTopicName(IAssembly assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);

    var reflectionAssembly = assembly.Assembly;

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

    return GetTopicName(assembly) + DeadLetterTopicSuffixName;
  }

  // =========================================================
  // EVENT MODE — event type-based (one topic per event type)
  // Derives topic name from the event type name using kebab-case.
  // HeroCreatedEvent   → hero-created-in
  // SkillAssignedEvent → skill-assigned-in
  // =========================================================

  public static string GetTopicName(Type eventType)
  {
    ArgumentNullException.ThrowIfNull(eventType);
    return ToKebabCase(StripEventSuffix(eventType.Name)) + TopicSuffixName;
  }

  public static string GetDeadLetterTopicName(Type eventType)
  {
    ArgumentNullException.ThrowIfNull(eventType);
    return ToKebabCase(StripEventSuffix(eventType.Name)) + TopicSuffixName + "-dlt";
  }

  // =========================================================
  // HELPERS
  // =========================================================

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
      return parts[1].ToLowerInvariant();

    return assemblyName.ToLowerInvariant();
  }

  /// <summary>
  /// Strips the "Event" suffix from an event type name if present.
  /// HeroCreatedEvent → HeroCreated
  /// HeroCreated      → HeroCreated (unchanged)
  /// </summary>
  private static string StripEventSuffix(string name)
      => name.EndsWith("Event", StringComparison.Ordinal)
          ? name[..^5]
          : name;

  /// <summary>
  /// Converts PascalCase to kebab-case.
  /// HeroCreated  → hero-created
  /// SkillAssigned → skill-assigned
  /// </summary>
  private static string ToKebabCase(string input)
      => Regex.Replace(input, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
}