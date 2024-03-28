using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Headers;

public interface IHeaderContextAccessor
{
  IEnumerable<KeyValuePair<string, StringValues>> ListAll();

  bool TryGetValue(string key, out StringValues value);

  bool TryGetValue<T>(string key, [MaybeNull] out T value);
}
