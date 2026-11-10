using Franz.Common.Serialization;
using Franz.Common.Serialization.Converters;
using Franz.Common.Serialization.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Franz.Common.Serialization.AotSmokeTest;

internal static class SerializationAotSmoke
{
  public static async Task<int> RunAsync()
  {
    var services = new ServiceCollection();
    services.AddSingleton<JsonConverter>(new EnumerationValueJsonConverter<AotStatus, int>());
    services.AddSerializers(AotJsonContext.Default);

    using var provider = services.BuildServiceProvider();
    var serializer = provider.GetRequiredService<IJsonSerializer>();
    var payload = new AotPayload(42, AotStatus.Ready);

    var json = serializer.Serialize(payload);
    if (string.IsNullOrWhiteSpace(json))
      return 1;

    var result = serializer.Deserialize<AotPayload>(json);
    if (result is null || result.Id != payload.Id || result.Status != AotStatus.Ready)
      return 2;

    var runtimeResult = serializer.Deserialize(json, typeof(AotPayload)) as AotPayload;
    if (runtimeResult is null || runtimeResult.Status != AotStatus.Ready)
      return 3;

    var byteSerializer = provider.GetRequiredService<IByteArraySerializer>();
    var bytes = byteSerializer.Serialize(payload);
    var byteResult = byteSerializer.Deserialize<AotPayload>(bytes);
    if (byteResult is null || byteResult.Status != AotStatus.Ready)
      return 4;

    try
    {
      _ = new SystemTextJsonSerializer(Array.Empty<JsonConverter>());
      return 5;
    }
    catch (InvalidOperationException)
    {
      // The reflection-only constructor must fail closed in Native AOT.
    }

    await Task.CompletedTask;
    return 0;
  }
}