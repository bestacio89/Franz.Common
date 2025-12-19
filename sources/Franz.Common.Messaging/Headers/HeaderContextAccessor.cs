using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Messaging.Headers;

public sealed class HeaderContextAccessor : IHeaderContextAccessor
{
  private readonly IMessageContextAccessor messageContextAccessor;

  public HeaderContextAccessor(IMessageContextAccessor messageContextAccessor)
  {
    this.messageContextAccessor = messageContextAccessor;
  }

  public IEnumerable<KeyValuePair<string, StringValues>> ListAll()
  {
    var headers = messageContextAccessor.Current?.Message?.Headers;

    return headers ?? Enumerable.Empty<KeyValuePair<string, StringValues>>();
  }

  public bool TryGetValue(string key, out StringValues value)
  {
    value = default;

    var headers = messageContextAccessor.Current?.Message?.Headers;
    if (headers == null)
      return false;

    return headers.TryGetValue(key, out value);
  }

  public bool TryGetValue<T>(string key, [MaybeNull] out T value)
  {
    value = default;

    var headers = messageContextAccessor.Current?.Message?.Headers;
    if (headers == null)
      return false;

    if (!headers.TryGetValue(key, out var stringValues))
      return false;

    // StringValues → string → T
#pragma warning disable CS8604
    value = JsonConvert.DeserializeObject<T>(stringValues.ToString());
#pragma warning restore CS8604

    return true;
  }
}
