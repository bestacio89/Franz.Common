#nullable enable
using Franz.Common.Reflection;
using System.Reflection;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.RabbitMQ;

public static class QueueNamer
{
  private const string QueueSuffixName = "-in";
  private const string DeadLetterQueueSuffixName = "-in-dlq";

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

  /// <summary>
  /// Routes to generic infrastructure queues based on MessageKind.
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
    return assembly.ExtractParts(2, "-").ToLowerInvariant();
  }
}
