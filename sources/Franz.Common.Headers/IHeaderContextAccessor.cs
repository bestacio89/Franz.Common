#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Headers;

/// <summary>
/// Provides a transport-agnostic interface for accessing message headers.
/// Senior Note: Migrated from StringValues to IDictionary/string[] to ensure 
/// cross-platform JSON serialization compatibility.
/// </summary>
public interface IHeaderContextAccessor
{
  /// <summary>
  /// Returns all headers in the current context as a serializable dictionary.
  /// </summary>
  IDictionary<string, string[]> ListAll();

  /// <summary>
  /// Attempts to retrieve a raw string array for the specified header key.
  /// </summary>
  bool TryGetValue(string key, [NotNullWhen(true)] out string[]? value);

  /// <summary>
  /// Attempts to retrieve and deserialize a header value to the specified type <typeparamref name="T"/>.
  /// </summary>
  /// <returns>True if the header exists and is compatible with type T; otherwise, false.</returns>
  bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value);
}