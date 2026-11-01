namespace Franz.Common.Business.Domain;

/// <summary>
/// Base entity for domain objects that require provider-independent optimistic concurrency.
/// </summary>
/// <typeparam name="TKey">The entity identifier type.</typeparam>
public abstract class ConcurrencyTrackedEntity<TKey> : Entity<TKey>, IConcurrencyTrackedEntity
{
    protected ConcurrencyTrackedEntity()
    {
    }

    protected ConcurrencyTrackedEntity(TKey id)
        : base(id)
    {
    }

    /// <inheritdoc />
    public long ConcurrencyVersion { get; private set; }
}
