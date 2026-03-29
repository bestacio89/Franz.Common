#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Franz.Common.Messaging.Headers;

/// <summary>
/// Provides access to headers within the current message context.
/// Senior Note: Refactored to eliminate StringValues in favor of serializable string arrays.
/// </summary>
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

    // Returns a fresh case-insensitive dictionary to ensure upstream safety
    return headers ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Retrieves the raw string array for a specific header key.
  /// </summary>
  public bool TryGetValue(string key, [NotNullWhen(true)] out string[]? value)
  {
    value = default;

    var headers = _messageContextAccessor.Current?.Message?.Headers;
    if (headers is null)
      return false;

    return headers.TryGetValue(key, out value);
  }

  /// <summary>
  /// Deserializes a header value to the specified type T.
  /// </summary>
  public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value)
  {
    value = default;

    if (!TryGetValue(key, out var values) || values.Length == 0)
      return false;

    // Messaging standard: retrieve the primary value from the first array element
    var raw = values[0];
    if (string.IsNullOrWhiteSpace(raw))
      return false;

    try
    {
      // Performance Optimization: Direct cast if T is string to bypass JSON overhead
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
      // Header found but format is incompatible with T
      return false;
    }
  }
}