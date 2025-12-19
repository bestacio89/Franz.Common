using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Messaging.Headers;

public class HeaderContextAccessor : IHeaderContextAccessor
{
  private readonly IMessageContextAccessor messageContextAccessor;

  public HeaderContextAccessor(IMessageContextAccessor messageContextAccessor)
  {
    this.messageContextAccessor = messageContextAccessor;
  }

  public IEnumerable<KeyValuePair<string, StringValues>> ListAll()
  {
    var headers = messageContextAccessor.Current?.Message?.Headers;

    if (headers == null)
      return Enumerable.Empty<KeyValuePair<string, StringValues>>();

    return headers.Select(kvp =>
        new KeyValuePair<string, StringValues>(
            kvp.Key,
            kvp.Value switch
            {
              StringValues sv => sv,
              IEnumerable<string> values => new StringValues(values.ToArray()),
              _ => StringValues.Empty
            }
        ));
  }




  public bool TryGetValue<T>(string key, [MaybeNull] out T value)
  {
    value = default;

    var headers = messageContextAccessor.Current?.Message?.Headers;
    if (headers == null)
      return false;

    if (!headers.TryGetValue(key, out var rawValues))
      return false;

    // Adapt IReadOnlyCollection<string> → StringValues
    var stringValues = rawValues switch
    {
      StringValues sv => sv,
      IEnumerable<string> values => new StringValues(values.ToArray()),
      _ => StringValues.Empty
    };

#pragma warning disable CS8604
    value = JsonConvert.DeserializeObject<T>(stringValues.ToString());
#pragma warning restore CS8604

    return true;
  }

}
