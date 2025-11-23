using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Abstraction over the Franz validation engine used by gRPC server behaviors.
/// Your existing validation engine can be adapted to this interface in DI.
/// </summary>
public interface IFranzValidationEngine
{
  /// <summary>
  /// Validates the given instance.
  /// Implementations are expected to throw a validation-specific exception
  /// when the instance is invalid (your existing Franz validation exception type).
  /// </summary>
  /// <typeparam name="T">The instance type to validate.</typeparam>
  /// <param name="instance">The instance to validate.</param>
  /// <param name="ruleSet">
  /// Optional rule set, for example the gRPC method name or a logical profile.
  /// Implementations may ignore this parameter if not needed.
  /// </param>
  /// <param name="cancellationToken">Cancellation token.</param>
  Task ValidateAsync<T>(
      T instance,
      string? ruleSet = null,
      CancellationToken cancellationToken = default)
      where T : class;
}
