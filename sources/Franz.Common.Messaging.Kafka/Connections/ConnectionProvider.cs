#nullable enable
using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Kafka.Connections;

public sealed class ConnectionProvider(IConnectionFactoryProvider connectionFactoryProvider)
    : IConnectionProvider, IAsyncDisposable, IDisposable
{
  private IProducer<string, object>? _connection;
  private readonly SemaphoreSlim _semaphore = new(1, 1);
  private bool _disposed;

  public IProducer<string, object> Current => GetCurrent();

  public IProducer<string, object> GetCurrent()
  {
    if (_connection is { } existingConnection) return existingConnection;

    _semaphore.Wait();
    try
    {
      if (_connection is null)
      {
        var config = connectionFactoryProvider.Current;

        if (config is null || string.IsNullOrWhiteSpace(config.BootstrapServers))
        {
          throw new ArgumentException("Kafka BootstrapServers must be configured.", nameof(connectionFactoryProvider));
        }

        // FIX: Added ValueSerializer to handle 'object' types. 
        // Defaulting to a Null/ByteArray approach to satisfy the Build() method.
        _connection = new ProducerBuilder<string, object>(config)
            .SetKeySerializer(Serializers.Utf8)
            .SetValueSerializer(new DefaultObjectSerializer())
            .Build();
      }
      return _connection;
    }
    finally
    {
      _semaphore.Release();
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed) return;
    if (_connection is not null)
    {
      await Task.Run(() =>
      {
        _connection.Flush(TimeSpan.FromSeconds(10));
        _connection.Dispose();
      }).ConfigureAwait(false);
    }
    _semaphore.Dispose();
    _disposed = true;
    GC.SuppressFinalize(this);
  }

  public void Dispose()
  {
    if (_disposed) return;
    _connection?.Flush(TimeSpan.FromSeconds(10));
    _connection?.Dispose();
    _semaphore.Dispose();
    _disposed = true;
  }

  // INTERNAL SERIALIZER: Required for ProducerBuilder<string, object>
  private sealed class DefaultObjectSerializer : ISerializer<object>
  {
    public byte[] Serialize(object data, SerializationContext context) =>
        data switch
        {
          byte[] bytes => bytes,
          string str => System.Text.Encoding.UTF8.GetBytes(str),
          null => Array.Empty<byte>(),
          _ => System.Text.Encoding.UTF8.GetBytes(data.ToString() ?? string.Empty)
        };
  }
}