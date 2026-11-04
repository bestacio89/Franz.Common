#nullable enable
using Franz.Common.Messaging.Messages;
using Franz.Common.Reflection;
using System.Reflection;

namespace Franz.Common.Messaging.RabbitMQ;

/// <summary>
/// Resolves RabbitMQ queue names and generic message-kind destinations.
/// </summary>
internal static class RabbitMQQueueNamer
{
  private const string QueueSuffixName = "-in";
  private const string DeadLetterQueueSuffixName = "-in-dlq";

  public static string GetQueueName(Assembly assembly)
      => GetQueueName(new AssemblyWrapper(assembly));

  public static string GetQueueName(IAssembly assembly)
      => string.Concat(GetServiceName(assembly), QueueSuffixName);

  public static string GetDeadLetterQueueName(Assembly assembly)
      => GetDeadLetterQueueName(new AssemblyWrapper(assembly));

  public static string GetDeadLetterQueueName(IAssembly assembly)
      => string.Concat(GetServiceName(assembly), DeadLetterQueueSuffixName);

  public static string GetQueueName(MessageKind kind)
      => kind switch
      {
        MessageKind.Command => "franz-commands-in",
        MessageKind.Query => "franz-queries-in",
        MessageKind.Fault => "franz-faults-in",
        _ => "franz-events-in"
      };

  private static string GetServiceName(IAssembly assembly)
      => assembly.ExtractParts(2, "-").ToLowerInvariant();
}
