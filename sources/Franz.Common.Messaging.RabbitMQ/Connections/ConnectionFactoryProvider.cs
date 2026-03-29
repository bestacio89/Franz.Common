#nullable enable
using Franz.Common.Messaging.Configuration;
using Franz.Common.Reflection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Net.Security;
using System.Security.Authentication;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public sealed class ConnectionFactoryProvider : IConnectionFactoryProvider
{
  private readonly IOptions<RabbitMQMessagingOptions> _options;
  private readonly IAssemblyAccessor _assemblyAccessor;

  public ConnectionFactoryProvider(
      IOptions<RabbitMQMessagingOptions> options,
      IAssemblyAccessor assemblyAccessor)
  {
    _options = options;
    _assemblyAccessor = assemblyAccessor;
  }

  public IConnectionFactory Current => CreateFactory();

  private IConnectionFactory CreateFactory()
  {
    var options = _options.Value;

    // ✅ Preferred: URI-based connection (Testcontainers / modern usage)
    if (!string.IsNullOrWhiteSpace(options.BootStrapServers))
    {
      return new ConnectionFactory
      {
        Uri = new Uri(options.BootStrapServers),
        ClientProvidedName = GetClientProvidedName(),

        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        RequestedHeartbeat = TimeSpan.FromSeconds(30)
      };
    }

    // ⚠️ Fallback: legacy host/port configuration
    var factory = new ConnectionFactory
    {
      HostName = options.HostName ?? "localhost",
      UserName = options.UserName ?? "guest",
      Password = options.Password ?? "guest",
      Port = options.Port ?? AmqpTcpEndpoint.UseDefaultPort,
      VirtualHost = options.VirtualHost ?? "/",

      ClientProvidedName = GetClientProvidedName(),

      AutomaticRecoveryEnabled = true,
      TopologyRecoveryEnabled = true,
      RequestedHeartbeat = TimeSpan.FromSeconds(30)
    };

    if (options.SslEnabled == true)
    {
      factory.Ssl = new SslOption
      {
        Enabled = true,
        ServerName = options.HostName,
        AcceptablePolicyErrors =
              SslPolicyErrors.RemoteCertificateNotAvailable |
              SslPolicyErrors.RemoteCertificateNameMismatch |
              SslPolicyErrors.RemoteCertificateChainErrors,
        Version = SslProtocols.Tls12 | SslProtocols.Tls13
      };

      if (options.Port is null)
        factory.Port = 5671;
    }

    return factory;
  }

  private string? GetClientProvidedName()
  {
    var name = _assemblyAccessor.GetEntryAssembly()?.Name;

    if (!string.IsNullOrWhiteSpace(Environment.MachineName))
      name = $"{name} ({Environment.MachineName})";

    return name;
  }
}