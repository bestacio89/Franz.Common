using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Hosting.Security;

/// <summary>
/// Default gRPC authorization policy. Calls are denied until the host registers
/// an explicit authorization implementation.
/// </summary>
public sealed class DenyAllAuthorizationService : IFranzAuthorizationService
{
    public Task AuthorizeAsync(
        GrpcCallContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        throw new RpcException(new Status(
            StatusCode.PermissionDenied,
            "The gRPC call is not authorized."));
    }
}
