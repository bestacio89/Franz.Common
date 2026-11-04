using Franz.Common.Reflection;
using System.Reflection;

namespace Franz.Common.Messaging.RabbitMQ;

public static class ExchangeNamer
{
  private const string DeadLetterExchangeSuffixName = "-dlq";
  private const string EventExchangeSuffixName = "-event";
  private const string ReplayExchangeSuffixName = "-replay";

  public static string GetDeadLetterExchangeName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    return GetDeadLetterExchangeName(assemblyWrapper);
  }

  public static string GetDeadLetterExchangeName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);
    return string.Concat(serviceName, DeadLetterExchangeSuffixName);
  }

  public static string GetEventExchangeName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    return GetEventExchangeName(assemblyWrapper);
  }

  public static string GetEventExchangeName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);
    return string.Concat(serviceName, EventExchangeSuffixName);
  }

  public static string GetReplayExchangerName(Assembly assembly)
  {
    var assemblyWrapper = new AssemblyWrapper(assembly);
    return GetReplayExchangeName(assemblyWrapper);
  }

  public static string GetReplayExchangeName(IAssembly assembly)
  {
    var serviceName = GetServiceName(assembly);
    return string.Concat(serviceName, ReplayExchangeSuffixName);
  }

  private static string GetServiceName(IAssembly assembly)
  {
    var serviceName = assembly.ExtractParts(2, "-").ToLowerInvariant();
    return serviceName;
  }
}
