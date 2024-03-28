using Franz.Common.Reflection;
using System.Reflection;

namespace Franz.Common.Messaging.Kafka;

public static class TopicNamer
{
  private const string TopicSuffixName = "-in";
  private const string DeadLetterTopicSuffixName = "-in-dlt";

  public static string GetTopicName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    var result = GetTopicName(assemblyWrapper);

    return result;
  }

  public static string GetTopicName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);

    var result = string.Concat(serviceName, TopicSuffixName);

    return result;
  }

  public static string GetDeadLetterTopicName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    var result = GetDeadLetterTopicName(assemblyWrapper);

    return result;
  }

  public static string GetDeadLetterTopicName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);

    var result = string.Concat(serviceName, DeadLetterTopicSuffixName);

    return result;
  }

  private static string GetServiceName(IAssembly assembly)
  {
    var serviceName = assembly.ExtractParts(2, "-").ToLower();

    return serviceName;
  }
}
