using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Hosting.NoOp;

public sealed class NoOpAuthorizationService : IFranzAuthorizationService
{
  public Task AuthorizeAsync(GrpcCallContext context, CancellationToken cancellationToken = default)
  {
    // Always authorized
    return Task.CompletedTask;
  }
}
