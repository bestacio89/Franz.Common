using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Caching.Abstractions;
public interface ISettingsCache
{
  Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default);
  Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default);
}
