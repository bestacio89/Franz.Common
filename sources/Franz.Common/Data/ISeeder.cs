using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Franz.Common.DependencyInjection;

namespace Franz.Common.Data;

public interface ISeeder : IScopedDependency
{
  int Order { get; }

  Task SeedAsync(CancellationToken cancellationToken = default);
}