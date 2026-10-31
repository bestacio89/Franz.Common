using Confluent.Kafka;
using Franz.Common.Errors;
using Franz.Common.Messaging.MassTransit.Contracts;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace Franz.Common.Messaging.MassTransit;

public class KafkaConsumer : IKafkaConsumer
{
  private readonly IConsumer<string, string> _consumer;
  private readonly IServiceScopeFactory? _scopeFactory;

  public KafkaConsumer(
      IConsumer<string, string> consumer,
      IServiceScopeFactory? scopeFactory = null)
  {
    _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    _scopeFactory = scopeFactory;
  }

  public Task PublishAsync<T>(T message, string topic, CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException("Consumer cannot publish.");
  }

  public async Task SubscribeAsync<T>(
      string topic,
      Func<T, Task> handler,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(handler);
    _consumer.Subscribe(topic);

    while (!cancellationToken.IsCancellationRequested)
    {
      var result = ConsumeRequired(topic, cancellationToken);
      var message = DeserializeRequired<T>(result, topic);

      if (_scopeFactory is null)
      {
        await handler(message).ConfigureAwait(false);
        continue;
      }

      await using var scope = _scopeFactory.CreateAsyncScope();
      if (scope.ServiceProvider.GetService<ITenantMessageContextInitializer>() is not null)
      {
        throw new InvalidOperationException(
            "Tenant-aware Kafka processing requires the scoped handler overload: " +
            "SubscribeAsync<T>(topic, Func<IServiceProvider, T, Task>, cancellationToken).");
      }

      await handler(message).ConfigureAwait(false);
    }
  }

  public async Task SubscribeAsync<T>(
      string topic,
      Func<IServiceProvider, T, Task> handler,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(handler);

    if (_scopeFactory is null)
      throw new InvalidOperationException(
          "The scoped Kafka handler overload requires an IServiceScopeFactory.");

    _consumer.Subscribe(topic);

    while (!cancellationToken.IsCancellationRequested)
    {
      var result = ConsumeRequired(topic, cancellationToken);
      var payload = DeserializeRequired<T>(result, topic);

      await using var scope = _scopeFactory.CreateAsyncScope();
      var tenantInitializer = scope.ServiceProvider.GetService<ITenantMessageContextInitializer>();

      if (tenantInitializer is null)
      {
        await handler(scope.ServiceProvider, payload).ConfigureAwait(false);
        continue;
      }

      var tenantMessage = CreateTenantMessage(result);
      await using var tenantExecutionScope = await tenantInitializer
          .BeginAsync(tenantMessage, cancellationToken).ConfigureAwait(false);

      await handler(scope.ServiceProvider, payload).ConfigureAwait(false);
    }
  }

  private static ConsumeResult<string, string> ConsumeRequired(
      string topic,
      CancellationToken cancellationToken)
  {
    var result = _consumer.Consume(cancellationToken);
    if (string.IsNullOrWhiteSpace(result.Message?.Value))
      throw new TechnicalException($"Kafka message on topic '{topic}' had no payload.");

    return result;
  }

  private static T DeserializeRequired<T>(ConsumeResult<string, string> result, string topic)
  {
    var message = JsonSerializer.Deserialize<T>(result.Message.Value);
    if (message is null)
      throw new TechnicalException(
          $"Failed to deserialize message on topic '{topic}' into type {typeof(T).Name}.");

    return message;
  }

  private static Message CreateTenantMessage(ConsumeResult<string, string> result)
  {
    var message = new Message(result.Message.Value);
    var headers = result.Message.Headers;
    if (headers is null)
      return message;

    foreach (var group in headers.GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase))
    {
      message.Headers[group.Key] = group
          .Select(header => Encoding.UTF8.GetString(header.GetValueBytes() ?? Array.Empty<byte>()))
          .ToArray();
    }

    return message;
  }
}
