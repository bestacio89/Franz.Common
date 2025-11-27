#nullable enable

using System;
using System.Text.Json;

namespace Franz.Common.Messaging.Sagas.Persistence.Serializer;

/// <summary>
/// Default saga state serializer using System.Text.Json.
/// </summary>
public sealed class JsonSagaStateSerializer : ISagaStateSerializer
{
  private readonly JsonSerializerOptions _options;

  public JsonSagaStateSerializer(JsonSerializerOptions? options = null)
  {
    _options = options ?? new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = false
    };
  }

  public string Serialize(object state)
      => JsonSerializer.Serialize(state, _options);

  public object Deserialize(string payload, Type targetType)
      => JsonSerializer.Deserialize(payload, targetType, _options)
         ?? throw new InvalidOperationException(
              $"Failed to deserialize saga state into {targetType.Name}");
}
