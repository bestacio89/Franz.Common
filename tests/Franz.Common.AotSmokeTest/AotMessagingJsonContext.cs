using System.Text.Json.Serialization;

namespace Franz.Common.AotSmokeTest;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AotIntegrationEvent))]
internal partial class AotMessagingJsonContext : JsonSerializerContext
{
}
