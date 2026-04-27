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

    var factory = new ConnectionFactory
    {
      HostName = Require(options.HostName, "localhost"),
      UserName = Require(options.UserName, "guest"),
      Password = Require(options.Password, "guest"),
      Port = options.Port ?? AmqpTcpEndpoint.UseDefaultPort,
      VirtualHost = Require(options.VirtualHost, "/"),

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
        ServerName = factory.HostName, // safer than options.HostName
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

  private static string Require(string? value, string fallback)
      => string.IsNullOrWhiteSpace(value) ? fallback : value;

  private string? GetClientProvidedName()
  {
    var name = _assemblyAccessor.GetEntryAssembly()?.Name;

    if (!string.IsNullOrWhiteSpace(Environment.MachineName))
      name = $"{name} ({Environment.MachineName})";

    return name;
  }
}