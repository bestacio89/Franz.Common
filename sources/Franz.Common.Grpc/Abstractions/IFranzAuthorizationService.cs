using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Defines the Franz authorization contract for gRPC server calls.
/// </summary>
public interface IFranzAuthorizationService
{
  /// <summary>
  /// Determines whether the current request is authorized.
  /// Implementations should throw a security exception if unauthorized.
  /// </summary>
  Task AuthorizeAsync(
      GrpcCallContext context,
      CancellationToken cancellationToken = default);
}
