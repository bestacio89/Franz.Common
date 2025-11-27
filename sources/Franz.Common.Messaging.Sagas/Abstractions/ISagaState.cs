#nullable enable

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Marker interface for saga state objects.
/// Concrete saga states are plain POCOs that implement this interface.
/// </summary>
public interface ISagaState
{
  /// <summary>
  /// Optional optimistic concurrency token (row version, etag, etc.).
  /// The persistence provider decides how to use it.
  /// </summary>
  string? ConcurrencyToken { get; set; }
}
