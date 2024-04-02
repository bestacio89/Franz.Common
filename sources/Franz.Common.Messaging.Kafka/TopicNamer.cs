using Franz.Common.Reflection;
using System.Reflection;
using System.Linq;

namespace Franz.Common.Messaging.Kafka
{
  public static class TopicNamer
  {
    private const string TopicSuffixName = "-in";
    private const string DeadLetterTopicSuffixName = "-in-dlt";

    public static string GetTopicName(Assembly assembly)
    {
      var serviceName = GetServiceName(assembly.GetName().Name);
      return string.Concat(serviceName, TopicSuffixName);
    }

    public static string GetDeadLetterTopicName(Assembly assembly)
    {
      var serviceName = GetServiceName(assembly.GetName().Name);
      return string.Concat(serviceName, DeadLetterTopicSuffixName);
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
  }
}
