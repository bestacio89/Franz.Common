#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Messaging.Headers;

/// <summary>Provides access to headers within the current message context.</summary>
public sealed class HeaderContextAccessor : IHeaderContextAccessor
{
  private readonly IMessageContextAccessor _messageContextAccessor;
  private readonly JsonSerializerOptions _jsonOptions;

  public HeaderContextAccessor(
      IMessageContextAccessor messageContextAccessor,
      JsonSerializerOptions? jsonOptions = null)
  {
    _messageContextAccessor = messageContextAccessor;
    _jsonOptions = jsonOptions ?? FranzJson.Default;
  }

  public IDictionary<string, string[]> ListAll()
  {
    var headers = _messageContextAccessor.Current?.Message?.Headers;
    return headers ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
  }

  public bool TryGetValue(string key, [NotNullWhen(true)] out string[]? value)
  {
    value = default;
    var headers = _messageContextAccessor.Current?.Message?.Headers;
    return headers is not null && headers.TryGetValue(key, out value);
  }

  [RequiresUnreferencedCode("Use the JsonTypeInfo overload for Native AOT.")]
  [RequiresDynamicCode("Use the JsonTypeInfo overload for Native AOT.")]
  public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value)
  {
    value = default;
    if (!TryGetRawValue(key, out var raw))
      return false;

    try
    {
      if (typeof(T) == typeof(string))
      {
        value = (T)(object)raw;
        return true;
      }

      value = JsonSerializer.Deserialize<T>(raw, _jsonOptions);
      return value is not null;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  /// <summary>Reads a typed header using application-provided source-generated JSON metadata.</summary>
  public bool TryGetValue<T>(
      string key,
      JsonTypeInfo<T> typeInfo,
      [MaybeNullWhen(false)] out T value)
  {
    ArgumentNullException.ThrowIfNull(typeInfo);
    value = default;
    if (!TryGetRawValue(key, out var raw))
      return false;

    try
    {
      if (typeof(T) == typeof(string))
      {
        value = (T)(object)raw;
        return true;
      }

      value = JsonSerializer.Deserialize(raw, typeInfo);
      return value is not null;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private bool TryGetRawValue(string key, [NotNullWhen(true)] out string? raw)
  {
    raw = null;
    if (!TryGetValue(key, out var values) || values.Length == 0)
      return false;

    raw = values[0];
    return !string.IsNullOrWhiteSpace(raw);
  }
}
