using Franz.Common.Business.Events;

namespace Franz.Common.Business.Domain;

public abstract class Entity<TId> : IEntity
{
  private int? requestedHashCode;
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public TId? Id { get; protected set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public DateTime DateCreated { get; set; }
  public DateTime LastModifiedDate { get; set; }
  public string CreatedBy { get; set; } = string.Empty;
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public string? LastModifiedBy { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public Guid PersistantId { get; set; }

  private readonly List<IDomainEvent> events;
  public IReadOnlyCollection<IDomainEvent> Events => events.AsReadOnly();

  public Entity()
  {
    events = new List<IDomainEvent>();
  }

  public void AddEvent(IDomainEvent eventItem)
  {
    events.Add(eventItem);
  }

  public void RemoveEvent(IDomainEvent eventItem)
  {
    events.Remove(eventItem);
  }

  public void ClearEvents()
  {
    events.Clear();
  }

  public bool IsTransient()
  {
    return EqualityComparer<TId>.Default.Equals(Id, default);
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public override bool Equals(object? obj)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    if (obj is not Entity<TId>)
    {
      return false;
    }

    if (ReferenceEquals(this, obj))
    {
      return true;
    }

    if (GetType() != obj.GetType())
    {
      return false;
    }

    var item = (Entity<TId>)obj;

    return !item.IsTransient() && !IsTransient() && item.Id!.Equals(Id);
  }

  public override int GetHashCode()
  {
    if (!IsTransient())
    {
      if (!requestedHashCode.HasValue)
      {
        requestedHashCode = Id!.GetHashCode() ^ 31; // XOR for random distribution (http://blogs.msdn.com/b/ericlippert/archive/2011/02/28/guidelines-and-rules-for-gethashcode.aspx)
      }

      return requestedHashCode.Value;
    }
    else
    {
      return base.GetHashCode();
    }
  }

  public static bool operator ==(Entity<TId> left, Entity<TId> right)
  {
    return Equals(left, null) ? Equals(right, null) : left.Equals(right);
  }

  public static bool operator !=(Entity<TId> left, Entity<TId> right)
  {
    return !(left == right);
  }
}

public abstract class Entity : Entity<int>
{
}
