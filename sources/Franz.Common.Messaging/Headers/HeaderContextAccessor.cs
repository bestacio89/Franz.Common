#nullable enable

using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Serialization;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Franz.Common.Messaging.Headers;

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

  public IEnumerable<KeyValuePair<string, StringValues>> ListAll()
  {
    var headers = _messageContextAccessor.Current?.Message?.Headers;

    return headers ?? Enumerable.Empty<KeyValuePair<string, StringValues>>();
  }

  public bool TryGetValue(string key, out StringValues value)
  {
    value = default;

    var headers = _messageContextAccessor.Current?.Message?.Headers;
    if (headers is null)
      return false;

    return headers.TryGetValue(key, out value);
  }

  public bool TryGetValue<T>(string key, [MaybeNull] out T value)
  {
    value = default;

    var headers = _messageContextAccessor.Current?.Message?.Headers;
    if (headers is null)
      return false;

    if (!headers.TryGetValue(key, out var stringValues))
      return false;

    var raw = stringValues.ToString();
    if (string.IsNullOrWhiteSpace(raw))
      return false;

    try
    {
      value = JsonSerializer.Deserialize<T>(raw, _jsonOptions);
      return value is not null;
    }
    catch (JsonException)
    {
      // Header exists but cannot be deserialized to T
      return false;
    }
  }
}
