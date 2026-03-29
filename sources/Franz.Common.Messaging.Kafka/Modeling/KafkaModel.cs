#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.KafKa.Modeling;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Kafka.Modeling;

// Senior Note: Define the specific types your microservice actually uses here 
// to ensure the Source Generator creates the optimized metadata.
[JsonSerializable(typeof(Message))] // Assuming 'Message' is your base messaging type
internal partial class KafkaJsonSerializerContext : JsonSerializerContext { }

public sealed class KafkaModel(IConnectionFactoryProvider connectionFactoryProvider) : IModel, IAsyncDisposable
{
    private readonly IProducer<string, byte[]> _producer = new ProducerBuilder<string, byte[]>(new ProducerConfig
    {
        BootstrapServers = connectionFactoryProvider.Current.BootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true
    }).Build();

    public async ValueTask Produce<TMessage>(string topic, TMessage message, CancellationToken cancel)
    {
        var serializedMessage = SerializeMessage(message);
        
        // We await the ProduceAsync natively. 
        // Confluent.Kafka's ProduceAsync returns a Task, which we wrap in ValueTask.
        await _producer.ProduceAsync(topic, new Message<string, byte[]> { Value = serializedMessage }, cancel);
    }

    private byte[] SerializeMessage<TMessage>(TMessage message)
    {
        // High-Performance: Using the Source Generator context instead of reflection.
        return JsonSerializer.SerializeToUtf8Bytes(message!, typeof(TMessage), KafkaJsonSerializerContext.Default);
    }

    public async ValueTask DisposeAsync()
    {
        // Flush ensures all messages in the internal Kafka queue are sent before closing.
        // We use Task.Run because Confluent.Kafka's Flush is blocking.
        await Task.Run(() => 
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        });
        
        GC.SuppressFinalize(this);
    }

    // Standard Dispose for backward compatibility if IModel requires it
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}