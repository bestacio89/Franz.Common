using System.Text.Json.Serialization;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(object))]
internal partial class RabbitMqJsonSerializerContext : JsonSerializerContext
{
}
