#nullable enable

using Confluent.Kafka;
using System;
namespace Franz.Common.Messaging.Kafka.Connections;
public sealed class ConnectionProvider : IConnectionProvider, IDisposable
{
  private readonly IConnectionFactoryProvider connectionFactoryProvider;
  private IProducer<string, object>? connection;

  public ConnectionProvider(IConnectionFactoryProvider connectionFactoryProvider)
  {
    this.connectionFactoryProvider = connectionFactoryProvider;
  }

  public IProducer<string, object> Current => GetCurrent();

  public IProducer<string, object> GetCurrent()
  {
    if (connection == null)
    {
      var config = connectionFactoryProvider.Current;
      connection = new ProducerBuilder<string, object>(config).Build();
    }

   
  return connection;
  }

  public void Dispose()
  {
    if (connection != null)
    {
      connection.Dispose();
    }
  }
}
