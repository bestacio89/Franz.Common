#nullable enable
using Franz.Common.Reflection;
using System.Reflection;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging;

public static class QueueNamer
{
  private const string QueueSuffixName = "-in";
  private const string DeadLetterQueueSuffixName = "-in-dlq";

  // --- Original Assembly-Based Logic (For Listeners/Initializers) ---

  public static string GetQueueName(Assembly assembly) => GetQueueName(new AssemblyWrapper(assembly));

  public static string GetQueueName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);
    return string.Concat(serviceName, QueueSuffixName);
  }

  public static string GetDeadLetterQueueName(Assembly assembly) => GetDeadLetterQueueName(new AssemblyWrapper(assembly));

  public static string GetDeadLetterQueueName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);
    return string.Concat(serviceName, DeadLetterQueueSuffixName);
  }

  // --- New Enum-Based Logic (For Senders/Direct Routing) ---

  /// <summary>
  /// Routes to generic infrastructure queues based on MessageKind.
  /// Senior Note: Used by Senders when the specific destination assembly isn't known.
  /// </summary>
  public static string GetQueueName(MessageKind kind)
  {
    return kind switch
    {
      MessageKind.Command => "franz-commands-in",
      MessageKind.Query => "franz-queries-in",
      MessageKind.Fault => "franz-faults-in",
      _ => "franz-events-in"
    };
  }

  private static string GetServiceName(IAssembly assembly)
  {
    // Extracting parts (e.g., "Franz.Common" -> "franz-common")
    return assembly.ExtractParts(2, "-").ToLower();
  }
}