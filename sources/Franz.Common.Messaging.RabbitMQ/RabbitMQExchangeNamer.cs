#nullable enable
using Franz.Common.Reflection;
using System.Reflection;

namespace Franz.Common.Messaging.RabbitMQ;

/// <summary>
/// Resolves RabbitMQ exchange names from the consuming service identity.
/// </summary>
internal static class RabbitMQExchangeNamer
{
  private const string DeadLetterExchangeSuffixName = "-dlq";
  private const string EventExchangeSuffixName = "-event";
  private const string ReplayExchangeSuffixName = "-replay";

  public static string GetDeadLetterExchangeName(Assembly assembly)
      => GetDeadLetterExchangeName(new AssemblyWrapper(assembly));

  public static string GetDeadLetterExchangeName(IAssembly assembly)
      => string.Concat(GetServiceName(assembly), DeadLetterExchangeSuffixName);

  public static string GetEventExchangeName(Assembly assembly)
      => GetEventExchangeName(new AssemblyWrapper(assembly));

  public static string GetEventExchangeName(IAssembly assembly)
      => string.Concat(GetServiceName(assembly), EventExchangeSuffixName);

  public static string GetReplayExchangeName(Assembly assembly)
      => GetReplayExchangeName(new AssemblyWrapper(assembly));

  public static string GetReplayExchangeName(IAssembly assembly)
      => string.Concat(GetServiceName(assembly), ReplayExchangeSuffixName);

  private static string GetServiceName(IAssembly assembly)
      => assembly.ExtractParts(2, "-").ToLowerInvariant();
}
