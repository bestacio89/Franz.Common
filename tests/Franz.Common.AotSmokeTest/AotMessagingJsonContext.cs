using System.Text.Json.Serialization;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.AotSmokeTest;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AotIntegrationEvent))]
[JsonSerializable(typeof(AotCommand))]
[JsonSerializable(typeof(AotQuery))]
[JsonSerializable(typeof(ExecutionFault))]
internal partial class AotMessagingJsonContext : JsonSerializerContext
{
}
