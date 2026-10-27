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
    var factory = new ConnectionFactory
    {
      ClientProvidedName = GetClientProvidedName(),
      AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled ?? true,
      TopologyRecoveryEnabled = options.TopologyRecoveryEnabled ?? true,
      RequestedHeartbeat = TimeSpan.FromSeconds(
          options.RequestedHeartbeatSeconds ?? 30)
    };

    if (!string.IsNullOrWhiteSpace(options.BootStrapServers))
    {
      if (!Uri.TryCreate(options.BootStrapServers, UriKind.Absolute, out var uri) ||
          (uri.Scheme != "amqp" && uri.Scheme != "amqps"))
      {
        throw new InvalidOperationException(
            "Messaging:RabbitMQ:BootStrapServers must be an absolute amqp:// or amqps:// URI.");
      }

      if (options.SslEnabled == true && uri.Scheme != "amqps")
      {
        throw new InvalidOperationException(
            "RabbitMQ TLS is enabled but BootStrapServers does not use the amqps:// scheme.");
      }

      factory.Uri = uri;
      return factory;
    }

    var sslEnabled = options.SslEnabled == true;
    factory.HostName = Require(options.HostName, "localhost");
    factory.UserName = Require(options.UserName, "guest");
    factory.Password = Require(options.Password, "guest");
    factory.VirtualHost = Require(options.VirtualHost, "/");
    factory.Port = options.Port ?? (sslEnabled ? 5671 : AmqpTcpEndpoint.UseDefaultPort);

    if (sslEnabled)
    {
      factory.Ssl = new SslOption
      {
        Enabled = true,
        ServerName = factory.HostName,
        // Never silently accept missing, mismatched, or untrusted server certificates.
        AcceptablePolicyErrors = SslPolicyErrors.None,
        Version = SslProtocols.Tls12 | SslProtocols.Tls13
      };
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