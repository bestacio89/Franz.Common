using Franz.Common.Reflection;
using System.Reflection;

namespace Franz.Common.Messaging.RabbitMQ;

public static class QueueNamer
{
  private const string QueueSuffixName = "-in";
  private const string DeadLetterQueueSuffixName = "-in-dlq";

  public static string GetQueueName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    var result = GetQueueName(assemblyWrapper);

    return result;
  }

  public static string GetQueueName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);

    var result = string.Concat(serviceName, QueueSuffixName);

    return result;
  }

  public static string GetDeadLetterQueueName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    var result = GetDeadLetterQueueName(assemblyWrapper);

    return result;
  }

  public static string GetDeadLetterQueueName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);

    var result = string.Concat(serviceName, DeadLetterQueueSuffixName);

    return result;
  }

  private static string GetServiceName(IAssembly assembly)
  {
    var serviceName = assembly.ExtractParts(2, "-").ToLower();

    return serviceName;
  }
}
