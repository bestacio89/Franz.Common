using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Headers;

public interface IHeaderContextAccessor
{
  IEnumerable<KeyValuePair<string, StringValues>> ListAll();



  bool TryGetValue<T>(string key, [MaybeNull] out T value);
}
