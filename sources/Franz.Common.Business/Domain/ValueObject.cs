namespace Franz.Common.Business.Domain;

public abstract class ValueObject<T> : IEquatable<T>
    where T : ValueObject<T>
{
  protected abstract IEnumerable<object?> GetEqualityComponents();

  public override bool Equals(object? obj)
  {
    if (obj is null || obj.GetType() != GetType())
      return false;

    return Equals((T)obj);
  }

  public bool Equals(T? other)
  {
    if (other is null)
      return false;

    return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
  }

  public override int GetHashCode()
  {
    return GetEqualityComponents()
        .Aggregate(17, (hash, obj) =>
        {
          unchecked
          {
            return hash * 31 + (obj?.GetHashCode() ?? 0);
          }
        });
  }

  public static bool operator ==(ValueObject<T>? left, ValueObject<T>? right)
      => Equals(left, right);

  public static bool operator !=(ValueObject<T>? left, ValueObject<T>? right)
      => !Equals(left, right);

  public T GetCopy() => (T)MemberwiseClone();
}

// Non-generic base if you want to keep the old-style as well
public abstract class ValueObject
{
  protected abstract IEnumerable<object?> GetEqualityComponents();

  public override bool Equals(object? obj)
  {
    if (obj is null || obj.GetType() != GetType())
      return false;

    var other = (ValueObject)obj;
    return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
  }

  public override int GetHashCode()
  {
    return GetEqualityComponents()
        .Aggregate(17, (hash, obj) =>
        {
          unchecked
          {
            return hash * 31 + (obj?.GetHashCode() ?? 0);
          }
        });
  }
}
