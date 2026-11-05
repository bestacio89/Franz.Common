#nullable enable
using Franz.Common.Reflection;
using System.Text.RegularExpressions;

namespace Franz.Common.Messaging.Kafka;

public static class TopicNamer
{
  private const string TopicSuffixName = "-in";
  private const string DeadLetterTopicSuffixName = "-in-dlt";

  /// <summary>Resolves the service topic from assembly identity without scanning runtime types.</summary>
  public static string GetTopicName(IAssembly assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    var assemblyName = assembly.Name;
    if (string.IsNullOrWhiteSpace(assemblyName))
      throw new InvalidOperationException("The entry assembly has no valid name.");
    return GetServiceName(assemblyName) + TopicSuffixName;
  }

  public static string GetDeadLetterTopicName(IAssembly assembly)
  {
    ArgumentNullException.ThrowIfNull(assembly);
    return GetTopicName(assembly) + DeadLetterTopicSuffixName;
  }

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

  private static string GetServiceName(string assemblyName)
  {
    if (assemblyName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
        assemblyName.Contains("vsts", StringComparison.OrdinalIgnoreCase))
      return "franz-test";
    var parts = assemblyName.Split('.');
    return parts.Length >= 2 ? parts[1].ToLowerInvariant() : assemblyName.ToLowerInvariant();
  }

  private static string StripEventSuffix(string name)
      => name.EndsWith("Event", StringComparison.Ordinal) ? name[..^5] : name;

  private static string ToKebabCase(string input)
      => Regex.Replace(input, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
}