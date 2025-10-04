using Franz.Common.Messaging.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Net.Security;
using System.Reflection;

namespace Franz.Common.Messaging.RabbitMQ.Connections;

public class ConnectionFactoryProvider : IConnectionFactoryProvider
{
  private readonly IOptions<MessagingOptions> messagingOptions;

  public ConnectionFactoryProvider(IOptions<MessagingOptions> messagingOptions)
  {
    this.messagingOptions = messagingOptions;
  }

  public IConnectionFactory Current => GetCurrent();

  private IConnectionFactory GetCurrent()
  {
    var result = new ConnectionFactory()
    {
      HostName = messagingOptions.Value.HostName ?? "localhost",
      UserName = messagingOptions.Value.UserName ?? "guest",
      Password = messagingOptions.Value.Password ?? "guest",
      Port = messagingOptions.Value.Port ?? 9092,
      VirtualHost = messagingOptions.Value.VirtualHost ?? "/",
      ClientProvidedName = GetClientProvidedName(),
    };

    if (messagingOptions.Value.SslEnabled != false)
    {
      result.Ssl = new SslOption
      {
        Enabled = true,
        AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors,
        Version = System.Security.Authentication.SslProtocols.Tls | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12
      };
    }

    return result;
  }

  private string? GetClientProvidedName()
  {
    var name = Assembly.GetEntryAssembly()?.GetName().Name;

    if (!string.IsNullOrEmpty(Environment.MachineName))
      name = string.Concat(name, " (", Environment.MachineName, ")");

    return name;
  }
}
