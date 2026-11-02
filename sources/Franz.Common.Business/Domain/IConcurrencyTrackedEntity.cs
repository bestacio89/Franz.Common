namespace Franz.Common.Business.Domain;

/// <summary>
/// Marks an entity as participating in framework-managed optimistic concurrency.
/// </summary>
public interface IConcurrencyTrackedEntity
{
    /// <summary>
    /// Monotonically increasing version used as an optimistic concurrency token.
    /// The persistence infrastructure owns updates to this value.
    /// </summary>
    long ConcurrencyVersion { get; }
}
