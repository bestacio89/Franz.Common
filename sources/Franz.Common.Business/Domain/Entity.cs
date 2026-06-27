using System;
using System.Collections.Generic;

namespace Franz.Common.Business.Domain;

public abstract class Entity<TKey> : IEntity<TKey>
{
  private int? _requestedHashCode;

  // =========================================================
  // Identity
  // =========================================================
  public TKey Id { get; private set; } = default!;

  protected Entity() { }

  protected Entity(TKey id)
  {
    Id = id;
  }

  // Only infrastructure access (Factories / EF)
  internal void SetId(TKey id)
  {
    Id = id;
  }

  public object GetId() => Id!;

  // =========================================================
  // Audit
  // =========================================================
  public DateTimeOffset DateCreated { get; private set; }
  public DateTimeOffset LastModifiedDate { get; private set; }

  public string CreatedBy { get; private set; } = string.Empty;
  public string? LastModifiedBy { get; private set; }

  public bool IsDeleted { get; private set; }
  public DateTimeOffset DateDeleted { get; private set; }
  public string? DeletedBy { get; private set; }

  // =========================================================
  // Lifecycle management
  // =========================================================
  public void MarkCreated(string createdBy)
  {
    DateCreated = DateTimeOffset.UtcNow;
    CreatedBy = createdBy;
  }

  public void MarkUpdated(string modifiedBy)
  {
    LastModifiedDate = DateTimeOffset.UtcNow;
    LastModifiedBy = modifiedBy;
  }

  public void MarkDeleted(string deletedBy)
  {
    IsDeleted = true;
    DateDeleted = DateTimeOffset.UtcNow;
    DeletedBy = deletedBy;
  }

  // =========================================================
  // Transience
  // =========================================================
  public bool IsTransient()
    => EqualityComparer<TKey>.Default.Equals(Id, default!);

  // =========================================================
  // Equality
  // =========================================================
  public override bool Equals(object? obj)
  {
    if (obj is not Entity<TKey> other)
      return false;

    if (ReferenceEquals(this, other))
      return true;

    if (GetType() != other.GetType())
      return false;

    if (IsTransient() || other.IsTransient())
      return false;

    return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
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

  public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    => left is null ? right is null : left.Equals(right);

  public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
    => !(left == right);
}