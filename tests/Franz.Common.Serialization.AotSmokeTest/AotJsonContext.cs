using System.Text.Json.Serialization;

namespace Franz.Common.Serialization.AotSmokeTest;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AotPayload))]
[JsonSerializable(typeof(AotStatus))]
[JsonSerializable(typeof(int))]
internal partial class AotJsonContext : JsonSerializerContext
{
}