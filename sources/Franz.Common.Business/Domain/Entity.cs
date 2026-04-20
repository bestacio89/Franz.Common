namespace Franz.Common.Business.Domain;

public abstract class Entity<TId> : IEntity
{
  private int? _requestedHashCode;

  public TId Id { get; private set; } = default!;

  protected Entity() { }

  protected Entity(TId id)
  {
    Id = id;
  }

  // ONLY infrastructure access (Factory / EF)
  internal void SetId(TId id)
  {
    Id = id;
  }

  public object GetId() => Id!;

  // -------------------------
  // Audit
  // -------------------------
  public DateTimeOffset DateCreated { get; private set; }
  public DateTimeOffset LastModifiedDate { get; private set; }
  public string CreatedBy { get; private set; } = string.Empty;
  public string? LastModifiedBy { get; private set; }

  public bool IsDeleted { get; private set; }
  public DateTimeOffset DateDeleted { get; private set; }
  public string? DeletedBy { get; private set; }

  public void MarkCreated(string createdBy)
  {
    DateCreated = DateTime.UtcNow;
    CreatedBy = createdBy;
  }

  public void MarkUpdated(string modifiedBy)
  {
    LastModifiedDate = DateTime.UtcNow;
    LastModifiedBy = modifiedBy;
  }

  public void MarkDeleted(string deletedBy)
  {
    IsDeleted = true;
    DateDeleted = DateTime.UtcNow;
    DeletedBy = deletedBy;
  }

  public bool IsTransient()
      => EqualityComparer<TId>.Default.Equals(Id, default!);

  public override bool Equals(object? obj)
  {
    if (obj is not Entity<TId> other)
      return false;

    if (ReferenceEquals(this, other))
      return true;

    if (GetType() != other.GetType())
      return false;

    if (IsTransient() || other.IsTransient())
      return false;

    return EqualityComparer<TId>.Default.Equals(Id, other.Id);
  }

  public override int GetHashCode()
  {
    if (!IsTransient())
    {
      _requestedHashCode ??= Id!.GetHashCode() ^ 31;
      return _requestedHashCode.Value;
    }

    return base.GetHashCode();
  }

  public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
      => left is null ? right is null : left.Equals(right);

  public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
      => !(left == right);
}

public abstract class Entity : Entity<int> { }