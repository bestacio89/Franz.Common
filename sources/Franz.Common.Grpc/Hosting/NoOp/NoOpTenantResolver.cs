using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Hosting.NoOp;

public sealed class NoOpTenantResolver : IFranzTenantResolver
{
  public Task<string?> ResolveTenantAsync(GrpcCallContext context, CancellationToken cancellationToken = default)
  {
    // No tenant resolution performed
    return Task.FromResult<string?>(null);
  }
}
