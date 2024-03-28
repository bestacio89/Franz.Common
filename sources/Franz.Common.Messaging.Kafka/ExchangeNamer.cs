using Franz.Common.Reflection;
using System.Reflection;

namespace Franz.Common.Messaging.Kafka;

public static class ExchangeNamer
{
  private const string DeadLetterExchangeSuffixName = "-dlq";
  private const string EventExchangeSuffixName = "-event";
  private const string ReplayExchangeSuffixName = "-replay";

  public static string GetDeadLetterExchangeName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    var result = GetDeadLetterExchangeName(assemblyWrapper);

    return result;
  }

  public static string GetDeadLetterExchangeName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);

    var result = string.Concat(serviceName, DeadLetterExchangeSuffixName);

    return result;
  }

  public static string GetEventExchangeName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    var result = GetEventExchangeName(assemblyWrapper);

    return result;
  }

  public static string GetEventExchangeName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);

    var result = string.Concat(serviceName, EventExchangeSuffixName);

    return result;
  }

  public static string GetReplayExchangerName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    var result = GetReplayExchangeName(assemblyWrapper);

    return result;
  }

  public static string GetReplayExchangeName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);

    var result = string.Concat(serviceName, ReplayExchangeSuffixName);

    return result;
  }

  private static string GetServiceName(IAssembly assembly)
  {
    var serviceName = assembly.ExtractParts(2, "-").ToLower();

    return serviceName;
  }
}
