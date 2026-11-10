#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Headers;

/// <summary>
/// Provides a transport-agnostic interface for accessing message headers.
/// </summary>
public interface IHeaderContextAccessor
{
  /// <summary>Returns all headers in the current context as a serializable dictionary.</summary>
  IDictionary<string, string[]> ListAll();

  /// <summary>Attempts to retrieve a raw string array for the specified header key.</summary>
  bool TryGetValue(string key, [NotNullWhen(true)] out string[]? value);

  /// <summary>
  /// Attempts to retrieve and deserialize a header value to the specified type <typeparamref name="T"/>.
  /// This compatibility overload uses the implementation's configured serializer.
  /// </summary>
  bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value);

  /// <summary>
  /// Attempts to retrieve and deserialize a header using application-provided source-generated metadata.
  /// </summary>
  bool TryGetValue<T>(
      string key,
      JsonTypeInfo<T> typeInfo,
      [MaybeNullWhen(false)] out T value)
  {
    ArgumentNullException.ThrowIfNull(typeInfo);
    value = default;

    if (!TryGetValue(key, out var values) || values.Length == 0 ||
        string.IsNullOrWhiteSpace(values[0]))
      return false;

    if (typeof(T) == typeof(string))
    {
      value = (T)(object)values[0];
      return true;
    }

    try
    {
      value = JsonSerializer.Deserialize(values[0], typeInfo);
      return value is not null;
    }
    catch (JsonException)
    {
      return false;
    }
  }
}
