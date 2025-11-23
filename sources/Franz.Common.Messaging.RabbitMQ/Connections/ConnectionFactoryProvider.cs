using Franz.Common.Messaging.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public sealed class ConnectionFactoryProvider : IConnectionFactoryProvider
{
  private readonly IOptions<MessagingOptions> _messagingOptions;

  public ConnectionFactoryProvider(IOptions<MessagingOptions> messagingOptions)
  {
    _messagingOptions = messagingOptions;
  }

  public IConnectionFactory Current => CreateFactory();

  private IConnectionFactory CreateFactory()
  {
    var options = _messagingOptions.Value;

    var factory = new ConnectionFactory
    {
      HostName = options.HostName ?? "localhost",
      UserName = options.UserName ?? "guest",
      Password = options.Password ?? "guest",

      Port = options.Port ?? AmqpTcpEndpoint.UseDefaultPort,
      VirtualHost = options.VirtualHost ?? "/",

      ClientProvidedName = GetClientProvidedName(),

      // Still valid in RabbitMQ 7.x:
      AutomaticRecoveryEnabled = true,
      TopologyRecoveryEnabled = true,
      RequestedHeartbeat = TimeSpan.FromSeconds(30),

      // NOTE: DispatchConsumersAsync REMOVED IN RABBITMQ 7.x
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

  private static string? GetClientProvidedName()
  {
    var name = Assembly.GetEntryAssembly()?.GetName().Name;

    if (!string.IsNullOrEmpty(Environment.MachineName))
      name = $"{name} ({Environment.MachineName})";

    return name;
  }
}
